using Avalonia.Controls;
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
        var win = new PatientWindow
        {
            DataContext = new PatientWindowViewModel(patientId)
        };

        win.Show();
    }
}