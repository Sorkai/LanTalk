using Avalonia.Controls;
using LanTalk.App.ViewModels;

namespace LanTalk.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.ShutdownAsync();
        }
    }
}
