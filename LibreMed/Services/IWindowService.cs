namespace LibreMed.Services;

public interface IWindowService
{
    void ShowVisitsWindow(int patientId);
    void ShowDiagnosisWindow(int visitId);
}