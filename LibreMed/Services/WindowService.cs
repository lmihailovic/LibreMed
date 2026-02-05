using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using LibreMed.ViewModels;
using LibreMed.Views;

namespace LibreMed.Services;

public sealed class WindowService : IWindowService
{
    public void ShowVisitsWindow(int patientId)
    {
        var win = new VisitsWindow
        {
            DataContext = new VisitsWindowViewModel(patientId)
        };

        // Optional: set owner if we can (nice for window stacking/focus)
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is not null)
        {
            win.Show(desktop.MainWindow);
            return;
        }

        win.Show();
    }
}