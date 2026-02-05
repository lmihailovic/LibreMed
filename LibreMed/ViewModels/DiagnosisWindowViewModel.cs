using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreMed.Data;
using LibreMed.Models;
using Microsoft.EntityFrameworkCore;

namespace LibreMed.ViewModels;

public partial class DiagnosisWindowViewModel : ViewModelBase
{
    private readonly int _visitId;

    [ObservableProperty] private string windowTitle = "Diagnosis";
    [ObservableProperty] private string header = "Diagnosis";
    [ObservableProperty] private string subheader = "";

    [ObservableProperty] private string titleText = string.Empty;
    [ObservableProperty] private string detailsText = string.Empty;
    [ObservableProperty] private string prescriptionText = string.Empty;

    public DiagnosisWindowViewModel(int visitId)
    {
        _visitId = visitId;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        using var db = CreateDbContext();

        var visit = await db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Diagnosis)
            .FirstAsync(v => v.Id == _visitId);

        WindowTitle = $"Visit #{visit.Id.ToString(CultureInfo.InvariantCulture)} — Diagnosis";
        Header = $"{visit.Patient?.Surname} {visit.Patient?.Name}";
        Subheader = $"Visit date: {visit.Date:yyyy-MM-dd}";

        TitleText = visit.Diagnosis?.Title ?? string.Empty;
        DetailsText = visit.Diagnosis?.Details ?? string.Empty;
        PrescriptionText = visit.Diagnosis?.Prescription ?? string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var title = (TitleText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
            return;

        var details = NormalizeOptional(DetailsText);
        var prescription = NormalizeOptional(PrescriptionText);

        using var db = CreateDbContext();

        var visit = await db.Visits
            .Include(v => v.Diagnosis)
            .FirstAsync(v => v.Id == _visitId);

        if (visit.Diagnosis is null)
        {
            visit.Diagnosis = new Diagnosis
            {
                VisitId = visit.Id,
                Title = title,
                Details = details,
                Prescription = prescription,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
        }
        else
        {
            visit.Diagnosis.Title = title;
            visit.Diagnosis.Details = details;
            visit.Diagnosis.Prescription = prescription;
            visit.Diagnosis.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        await RefreshAsync();
    }

    private static string? NormalizeOptional(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(v) ? null : v;
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