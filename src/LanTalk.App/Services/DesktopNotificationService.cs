using Avalonia.Threading;
using LanTalk.App.Views;
using LanTalk.Core.Services;

namespace LanTalk.App.Services;

public sealed class DesktopNotificationService : IDisposable
{
    private readonly Action _activateMainWindow;
    private readonly ILanTalkLogger _logger;
    private ToastNotificationWindow? _currentWindow;

    public DesktopNotificationService(Action activateMainWindow, ILanTalkLogger logger)
    {
        _activateMainWindow = activateMainWindow;
        _logger = logger;
    }

    public void Show(string title, string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _currentWindow?.Close();
                var window = new ToastNotificationWindow(title, message, _activateMainWindow);
                _currentWindow = window;
                window.Closed += (_, _) =>
                {
                    if (ReferenceEquals(_currentWindow, window))
                    {
                        _currentWindow = null;
                    }
                };
                window.Show();
            }
            catch (Exception ex)
            {
                _logger.Warning($"桌面通知显示失败：{ex.Message}");
            }
        });
    }

    public void Dispose()
    {
        _currentWindow?.Close();
        _currentWindow = null;
    }
}
