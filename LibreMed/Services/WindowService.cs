using System.Linq;
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
        var app = (App)Application.Current!;
        var fileSave = (IFileSaveDialogService)app.Services.GetService(typeof(IFileSaveDialogService))!;

        var win = new DiagnosisWindow
        {
            DataContext = new DiagnosisWindowViewModel(visitId, fileSave)
        };

        ShowOwnedOrStandalone(win);
    }

    public void CloseActiveWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var win = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.Windows.LastOrDefault();
        win?.Close();
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