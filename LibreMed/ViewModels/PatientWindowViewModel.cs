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

public partial class PatientWindowViewModel : ViewModelBase
{
    private readonly int _patientId;
    private readonly IWindowService _windowService;

    public ObservableCollection<VisitVm> Visits { get; } = new();

    [ObservableProperty] private string windowTitle = "Patient";
    [ObservableProperty] private string header = "Patient";
    [ObservableProperty] private string subheader = "";

    [ObservableProperty] private string patientIdText = "";
    [ObservableProperty] private string nameText = "";
    [ObservableProperty] private string surnameText = "";
    [ObservableProperty] private DateTimeOffset? birthDate = DateTimeOffset.Now;

    [ObservableProperty] private string phoneNumberText = "";
    [ObservableProperty] private string emailText = "";

    [ObservableProperty] private string generalNotesText = "";
    [ObservableProperty] private string medicalHistoryText = "";
    [ObservableProperty] private string familyHistoryText = "";

    [ObservableProperty] private string medicationsText = "";
    [ObservableProperty] private string allergiesText = "";

    [ObservableProperty] private DateTimeOffset? newVisitDate = DateTimeOffset.Now;
    [ObservableProperty] private string newVisitReason = string.Empty;
    [ObservableProperty] private string newVisitNotes = string.Empty;

    public PatientWindowViewModel(int patientId, IWindowService windowService)
    {
        _patientId = patientId;
        _windowService = windowService;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        using var db = CreateDbContext();

        var p = await db.Patients.AsNoTracking().FirstAsync(x => x.Id == _patientId);

        WindowTitle = $"{p.Surname} {p.Name} — LibreMed";
        Header = $"{p.Surname} {p.Name}";
        Subheader = $"Born: {p.BirthDate:yyyy-MM-dd}";

        PatientIdText = p.Id.ToString(CultureInfo.InvariantCulture);
        NameText = p.Name;
        SurnameText = p.Surname;
        BirthDate = new DateTimeOffset(p.BirthDate);

        PhoneNumberText = p.PhoneNumber ?? string.Empty;
        EmailText = p.Email ?? string.Empty;

        GeneralNotesText = p.GeneralNotes ?? string.Empty;
        MedicalHistoryText = p.MedicalHistory ?? string.Empty;
        FamilyHistoryText = p.FamilyHistory ?? string.Empty;

        MedicationsText = p.Medications is null ? string.Empty : string.Join(", ", p.Medications);
        AllergiesText = p.Allergies is null ? string.Empty : string.Join(", ", p.Allergies);

        var visits = await db.Visits
            .AsNoTracking()
            .Where(v => v.PatientId == _patientId)
            .OrderByDescending(v => v.Date)
            .ToListAsync();

        Visits.Clear();
        foreach (var v in visits)
            Visits.Add(VisitVm.FromModel(v));
    }

    [RelayCommand]
    private async Task SavePatientAsync()
    {
        var name = (NameText ?? string.Empty).Trim();
        var surname = (SurnameText ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname))
            return;

        var birth = (BirthDate ?? DateTimeOffset.Now).Date;

        using var db = CreateDbContext();
        var p = await db.Patients.FirstAsync(x => x.Id == _patientId);

        p.Name = name;
        p.Surname = surname;
        p.BirthDate = birth;

        p.PhoneNumber = NormalizeOptional(PhoneNumberText);
        p.Email = NormalizeOptional(EmailText);

        p.GeneralNotes = NormalizeOptional(GeneralNotesText);
        p.MedicalHistory = NormalizeOptional(MedicalHistoryText);
        p.FamilyHistory = NormalizeOptional(FamilyHistoryText);

        p.Medications = ParseCommaList(MedicationsText);
        p.Allergies = ParseCommaList(AllergiesText);

        await db.SaveChangesAsync();

        await RefreshAsync();
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
            Notes = notes
        });

        await db.SaveChangesAsync();

        NewVisitReason = string.Empty;
        NewVisitNotes = string.Empty;

        await RefreshAsync();
    }

    private static string? NormalizeOptional(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }

    private static System.Collections.Generic.List<string>? ParseCommaList(string? text)
    {
        var parts = (text ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return parts.Count == 0 ? null : parts;
    }

    private static AppDbContext CreateDbContext()
    {
        var dbPath = DbBootstrapper.GetDatabasePath();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new AppDbContext(options);
    }

    [RelayCommand]
    private void OpenVisits()
    {
        _windowService.ShowVisitsWindow(_patientId);
    }
}