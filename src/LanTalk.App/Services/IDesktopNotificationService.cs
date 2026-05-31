namespace LanTalk.App.Services;

public interface IDesktopNotificationService : IDisposable
{
    void Show(string title, string message);
}
