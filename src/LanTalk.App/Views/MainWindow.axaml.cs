using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using LanTalk.App.Services;
using LanTalk.App.ViewModels;
using LanTalk.Core.Services;

namespace LanTalk.App.Views;

public partial class MainWindow : Window
{
    private INotifyCollectionChanged? _messagesCollection;
    private MainWindowViewModel? _attachedViewModel;
    private TrayIcon? _trayIcon;
    private NativeMenuItem? _unreadMenuItem;
    private DesktopNotificationService? _notificationService;
    private bool _exitRequested;
    private bool _trayHintShown;

    public MainWindow()
    {
        InitializeComponent();
        InitializeTrayIcon();
        _notificationService = new DesktopNotificationService(RestoreFromTray, new ConsoleLanTalkLogger());
        Closing += OnClosing;
        DataContextChanged += OnDataContextChanged;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_exitRequested)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        DetachViewModel();
        DisposeTrayIcon();
        _notificationService?.Dispose();
        _notificationService = null;

        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.ShutdownAsync();
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        AttachViewModel(DataContext as MainWindowViewModel);
    }

    private void AttachViewModel(MainWindowViewModel? viewModel)
    {
        DetachViewModel();
        _attachedViewModel = viewModel;

        _messagesCollection = viewModel?.Messages;
        if (_messagesCollection is null)
        {
            return;
        }

        _messagesCollection.CollectionChanged += OnMessagesCollectionChanged;
        viewModel!.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.UserNotificationRequested += OnUserNotificationRequested;
        UpdateTrayState(viewModel);
        QueueScrollToLatestMessage();
    }

    private void DetachViewModel()
    {
        if (_messagesCollection is null)
        {
            return;
        }

        _messagesCollection.CollectionChanged -= OnMessagesCollectionChanged;
        _messagesCollection = null;

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _attachedViewModel.UserNotificationRequested -= OnUserNotificationRequested;
            _attachedViewModel = null;
        }
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        QueueScrollToLatestMessage();
    }

    private void QueueScrollToLatestMessage()
    {
        Dispatcher.UIThread.Post(MessagesScrollViewer.ScrollToEnd, DispatcherPriority.Loaded);
    }

    private void InitializeTrayIcon()
    {
        var showItem = new NativeMenuItem { Header = "打开 LanTalk" };
        showItem.Click += (_, _) => RestoreFromTray();

        _unreadMenuItem = new NativeMenuItem
        {
            Header = "没有未读消息",
            IsEnabled = false
        };

        var exitItem = new NativeMenuItem { Header = "退出" };
        exitItem.Click += (_, _) =>
        {
            _exitRequested = true;
            Close();
        };

        var menu = new NativeMenu();
        menu.Items.Add(showItem);
        menu.Items.Add(_unreadMenuItem);
        menu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = LoadTrayIcon(),
            ToolTipText = "LanTalk - 没有未读消息",
            Menu = menu,
            IsVisible = true
        };
        _trayIcon.Clicked += (_, _) => RestoreFromTray();
    }

    private static WindowIcon LoadTrayIcon()
    {
        var uri = new Uri("avares://LanTalk.App/Assets/avalonia-logo.ico");
        return new WindowIcon(AssetLoader.Open(uri));
    }

    private void HideToTray()
    {
        Hide();

        if (_trayHintShown)
        {
            return;
        }

        _trayHintShown = true;
        _notificationService?.Show("LanTalk 已在后台运行", "收到新消息时会通过托盘和桌面通知提醒。");
    }

    private void RestoreFromTray()
    {
        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainWindowViewModel viewModel &&
            e.PropertyName is nameof(MainWindowViewModel.TotalUnreadCount) or nameof(MainWindowViewModel.UnreadSummary))
        {
            UpdateTrayState(viewModel);
        }
    }

    private void OnUserNotificationRequested(object? sender, UserNotificationEventArgs e)
    {
        if (sender is MainWindowViewModel viewModel)
        {
            UpdateTrayState(viewModel);
        }

        if (IsVisible && IsActive && WindowState != WindowState.Minimized)
        {
            return;
        }

        _notificationService?.Show(e.Title, e.Message);
    }

    private void UpdateTrayState(MainWindowViewModel viewModel)
    {
        var text = viewModel.TotalUnreadCount == 0
            ? "没有未读消息"
            : $"{viewModel.TotalUnreadCount} 条未读消息";

        if (_unreadMenuItem is not null)
        {
            _unreadMenuItem.Header = text;
        }

        if (_trayIcon is not null)
        {
            _trayIcon.ToolTipText = $"LanTalk - {text}";
        }
    }

    private void DisposeTrayIcon()
    {
        if (_trayIcon is null)
        {
            return;
        }

        _trayIcon.IsVisible = false;
        _trayIcon.Dispose();
        _trayIcon = null;
    }
}
