using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
using LanTalk.App.ViewModels;

namespace LanTalk.App.Views;

public partial class MainWindow : Window
{
    private INotifyCollectionChanged? _messagesCollection;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
        DataContextChanged += OnDataContextChanged;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        DetachMessagesCollection();

        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.ShutdownAsync();
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        AttachMessagesCollection(DataContext as MainWindowViewModel);
    }

    private void AttachMessagesCollection(MainWindowViewModel? viewModel)
    {
        DetachMessagesCollection();

        _messagesCollection = viewModel?.Messages;
        if (_messagesCollection is null)
        {
            return;
        }

        _messagesCollection.CollectionChanged += OnMessagesCollectionChanged;
        QueueScrollToLatestMessage();
    }

    private void DetachMessagesCollection()
    {
        if (_messagesCollection is null)
        {
            return;
        }

        _messagesCollection.CollectionChanged -= OnMessagesCollectionChanged;
        _messagesCollection = null;
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        QueueScrollToLatestMessage();
    }

    private void QueueScrollToLatestMessage()
    {
        Dispatcher.UIThread.Post(MessagesScrollViewer.ScrollToEnd, DispatcherPriority.Loaded);
    }
}
