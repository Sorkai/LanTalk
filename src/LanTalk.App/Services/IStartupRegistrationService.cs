namespace LanTalk.App.Services;

public interface IStartupRegistrationService
{
    bool IsSupported { get; }

    bool IsEnabled();

    void SetEnabled(bool enabled);
}
