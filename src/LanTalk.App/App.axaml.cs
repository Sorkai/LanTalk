using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LanTalk.App.ViewModels;
using LanTalk.App.Views;
using LanTalk.Core.Services;

namespace LanTalk.App;

public partial class App : Application
{
    private readonly ConsoleLanTalkLogger _logger = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _logger.Info("Avalonia 应用资源初始化完成。");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += OnDesktopExit;
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            _logger.Info("LanTalk 桌面应用已完成主窗口初始化。");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _logger.Info("LanTalk 桌面应用正在退出。");
        _logger.Dispose();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger.Error("捕获到未处理异常。", e.ExceptionObject as Exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.Error("捕获到未观察到的任务异常。", e.Exception);
    }
}
