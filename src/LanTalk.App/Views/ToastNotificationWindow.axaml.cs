using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace LanTalk.App.Views;

public partial class ToastNotificationWindow : Window
{
    private readonly Action _activateMainWindow;
    private readonly DispatcherTimer _closeTimer;

    public ToastNotificationWindow()
        : this("LanTalk", "你有一条新消息。", () => { })
    {
    }

    public ToastNotificationWindow(string title, string message, Action activateMainWindow)
    {
        _activateMainWindow = activateMainWindow;
        DataContext = new ToastNotificationViewModel(title, message);
        InitializeComponent();

        Opened += OnOpened;
        PointerPressed += OnPointerPressed;

        _closeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(6)
        };
        _closeTimer.Tick += OnCloseTimerTick;
    }

    protected override void OnClosed(EventArgs e)
    {
        _closeTimer.Stop();
        _closeTimer.Tick -= OnCloseTimerTick;
        PointerPressed -= OnPointerPressed;
        Opened -= OnOpened;
        base.OnClosed(e);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        PositionNearWorkArea();
        _closeTimer.Start();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _activateMainWindow();
        Close();
    }

    private void OnCloseTimerTick(object? sender, EventArgs e)
    {
        Close();
    }

    private void PositionNearWorkArea()
    {
        var screen = Screens.Primary;
        if (screen is null)
        {
            return;
        }

        const int margin = 18;
        var area = screen.WorkingArea;
        Position = new Avalonia.PixelPoint(
            area.X + area.Width - (int)Width - margin,
            area.Y + area.Height - (int)Height - margin);
    }
}

public sealed record ToastNotificationViewModel(string Title, string Message);
