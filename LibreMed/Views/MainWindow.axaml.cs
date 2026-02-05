using Avalonia;
using Avalonia.Controls;
using LibreMed.Services;
using LibreMed.ViewModels;

namespace LibreMed.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (DataContext is MainWindowViewModel vm)
        {
            vm.RequestOpenPatientProfile += OpenPatientProfileWindow;
        }

        DataContextChanged += (_, __) =>
        {
            if (DataContext is MainWindowViewModel vm2)
                vm2.RequestOpenPatientProfile += OpenPatientProfileWindow;
        };
    }

    private void OpenPatientProfileWindow(int patientId)
    {
        var app = (App)Application.Current!;
        var windowService = (IWindowService)app.Services.GetService(typeof(IWindowService))!;

        var win = new PatientWindow
        {
            DataContext = new PatientWindowViewModel(patientId, windowService)
        };

        win.Show();
    }
}