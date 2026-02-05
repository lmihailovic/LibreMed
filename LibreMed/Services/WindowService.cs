using Avalonia;
using Avalonia.Controls;
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
            DataContext = new VisitsWindowViewModel(patientId, this)
        };

        ShowOwnedOrStandalone(win);
    }

    public void ShowDiagnosisWindow(int visitId)
    {
        var win = new DiagnosisWindow
        {
            DataContext = new DiagnosisWindowViewModel(visitId)
        };

        ShowOwnedOrStandalone(win);
    }

    private static void ShowOwnedOrStandalone(Window win)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is not null)
        {
            win.Show(desktop.MainWindow);
            return;
        }

        win.Show();
    }
}