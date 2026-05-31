namespace LanTalk.App.ViewModels;

public sealed class UserNotificationEventArgs : EventArgs
{
    public UserNotificationEventArgs(string title, string message, string sessionId)
    {
        Title = title;
        Message = message;
        SessionId = sessionId;
    }

    public string Title { get; }

    public string Message { get; }

    public string SessionId { get; }
}
