using Avalonia.Controls;
using Avalonia.Controls.Templates;
using LanTalk.App.ViewModels;

namespace LanTalk.App;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is MainWindowViewModel)
        {
            return new TextBlock { Text = "主窗口由应用启动器直接创建。" };
        }

        return param is null ? null : new TextBlock { Text = "未找到视图。" };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
