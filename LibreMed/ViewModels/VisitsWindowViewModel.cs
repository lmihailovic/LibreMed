using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreMed.Data;
using LibreMed.Models;
using LibreMed.Services;
using Microsoft.EntityFrameworkCore;

namespace LibreMed.ViewModels;

public partial class VisitsWindowViewModel : ViewModelBase
{
    private readonly int _patientId;
    private readonly IWindowService _windowService;

    public ObservableCollection<VisitRowVm> Visits { get; } = new();

    [ObservableProperty] private string windowTitle = "Visits";
    [ObservableProperty] private string header = "Visits";
    [ObservableProperty] private string subheader = "";

    [ObservableProperty] private DateTimeOffset? newVisitDate = DateTimeOffset.Now;
    [ObservableProperty] private string newVisitReason = string.Empty;
    [ObservableProperty] private string newVisitNotes = string.Empty;

    public VisitsWindowViewModel(int patientId, IWindowService windowService)
    {
        _patientId = patientId;
        _windowService = windowService;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void OpenDiagnosis(int visitId)
    {
        _windowService.ShowDiagnosisWindow(visitId);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        using var db = CreateDbContext();

        var p = await db.Patients.AsNoTracking().FirstAsync(x => x.Id == _patientId);

        WindowTitle = $"{p.Surname} {p.Name} — Visits";
        Header = $"{p.Surname} {p.Name}";
        Subheader = $"All visits (Patient #{p.Id.ToString(CultureInfo.InvariantCulture)})";

        var visits = await db.Visits
            .AsNoTracking()
            .Where(v => v.PatientId == _patientId)
            .OrderByDescending(v => v.Date)
            .ToListAsync();

        Visits.Clear();
        foreach (var v in visits)
        {
            Visits.Add(new VisitRowVm
            {
                Id = v.Id,
                Date = v.Date,
                Reason = v.Reason,
                Notes = v.Notes,
                OpenDiagnosisCommand = OpenDiagnosisCommand
            });
        }
    }

    [RelayCommand]
    private async Task AddVisitAsync()
    {
        var reason = (NewVisitReason ?? string.Empty).Trim();
        var notes = (NewVisitNotes ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return;

        var date = (NewVisitDate ?? DateTimeOffset.Now).Date;

        using var db = CreateDbContext();

        db.Visits.Add(new Visit
        {
            PatientId = _patientId,
            Date = date,
            Reason = reason,
            Notes = notes,
        });

        await db.SaveChangesAsync();

        NewVisitReason = string.Empty;
        NewVisitNotes = string.Empty;

        await RefreshAsync();
    }

    private static AppDbContext CreateDbContext()
    {
        var dbPath = DbBootstrapper.GetDatabasePath();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new AppDbContext(options);
    }
}

public sealed class VisitRowVm
{
    public required int Id { get; init; }
    public required DateTime Date { get; init; }
    public required string Reason { get; init; }
    public required string Notes { get; init; }

    public required IRelayCommand<int> OpenDiagnosisCommand { get; init; }

    public string WhenText => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}