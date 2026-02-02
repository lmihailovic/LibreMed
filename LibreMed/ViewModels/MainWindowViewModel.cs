using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreMed.Data;
using LibreMed.Models;
using Microsoft.EntityFrameworkCore;

namespace LibreMed.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<PatientListItemVm> Patients { get; } = new();
    public ObservableCollection<VisitVm> UpcomingVisits { get; } = new();

    [ObservableProperty]
    private PatientListItemVm? selectedPatient;

    [ObservableProperty]
    private string newPatientName = string.Empty;

    [ObservableProperty]
    private string newPatientSurname = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? newPatientBirthDate = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset? newVisitDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string newVisitReason = string.Empty;

    [ObservableProperty]
    private string newVisitNotes = string.Empty;

    public bool CanCreateVisit => SelectedPatient is not null;

    public string SelectedPatientHeader =>
        SelectedPatient is null ? "Select a patient" : SelectedPatient.FullName;

    public string SelectedPatientSubheader =>
        SelectedPatient is null ? "Choose a patient from the list to view and add visits." : SelectedPatient.BirthDateText;

    public MainWindowViewModel()
    {
        _ = RefreshAsync();
    }

    partial void OnSelectedPatientChanged(PatientListItemVm? value)
    {
        OnPropertyChanged(nameof(CanCreateVisit));
        OnPropertyChanged(nameof(SelectedPatientHeader));
        OnPropertyChanged(nameof(SelectedPatientSubheader));

        _ = LoadUpcomingVisitsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            using var db = CreateDbContext();

            var patients = await db.Patients
                .AsNoTracking()
                .OrderBy(p => p.Surname)
                .ThenBy(p => p.Name)
                .ToListAsync();

            Patients.Clear();
            foreach (var p in patients)
                Patients.Add(PatientListItemVm.FromModel(p));

            // Keep selection if possible
            if (SelectedPatient is not null)
            {
                SelectedPatient = Patients.FirstOrDefault(x => x.Id == SelectedPatient.Id);
            }

            await LoadUpcomingVisitsAsync();
        }
        catch (Exception ex)
        {
            // Minimal: swallow into UI-friendly state later if you add a StatusBar / dialog service.
            Console.Error.WriteLine(ex);
        }
    }

    [RelayCommand]
    private async Task CreatePatientAsync()
    {
        var name = (NewPatientName ?? string.Empty).Trim();
        var surname = (NewPatientSurname ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname))
            return;

        var birthDate = (NewPatientBirthDate ?? DateTimeOffset.Now).Date;

        using var db = CreateDbContext();

        var patient = new Patient
        {
            Name = name,
            Surname = surname,
            BirthDate = birthDate,
            PhoneNumber = null,
            Email = null,
            GeneralNotes = null,
            MedicalHistory = null,
            FamilyHistory = null,
            Medications = null,
            Allergies = null
        };

        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        NewPatientName = string.Empty;
        NewPatientSurname = string.Empty;

        await RefreshAsync();
        SelectedPatient = Patients.FirstOrDefault(p => p.Id == patient.Id);
    }

    [RelayCommand]
    private async Task CreateVisitAsync()
    {
        if (SelectedPatient is null)
            return;

        var reason = (NewVisitReason ?? string.Empty).Trim();
        var notes = (NewVisitNotes ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return;

        var date = (NewVisitDate ?? DateTimeOffset.Now).Date;

        using var db = CreateDbContext();

        // Attach by FK; no need to load the Patient entity.
        var visit = new Visit
        {
            PatientId = SelectedPatient.Id,
            Date = date,
            Reason = reason,
            Notes = notes,
            Prescription = string.Empty,
            Patient = null! // EF will use PatientId; required nav can remain null at runtime here
        };

        db.Visits.Add(visit);
        await db.SaveChangesAsync();

        NewVisitReason = string.Empty;
        NewVisitNotes = string.Empty;

        await LoadUpcomingVisitsAsync();
    }

    private async Task LoadUpcomingVisitsAsync()
    {
        UpcomingVisits.Clear();

        if (SelectedPatient is null)
            return;

        using var db = CreateDbContext();

        var fromDate = DateTime.Today;

        var visits = await db.Visits
            .AsNoTracking()
            .Where(v => v.PatientId == SelectedPatient.Id && v.Date >= fromDate)
            .OrderBy(v => v.Date)
            .ToListAsync();

        foreach (var v in visits)
            UpcomingVisits.Add(VisitVm.FromModel(v));
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

public sealed class PatientListItemVm
{
    public required int Id { get; init; }
    public required string FullName { get; init; }
    public required DateTime BirthDate { get; init; }

    public string BirthDateText => $"Born: {BirthDate:yyyy-MM-dd}";

    public static PatientListItemVm FromModel(Patient p) => new()
    {
        Id = p.Id,
        FullName = $"{p.Surname} {p.Name}",
        BirthDate = p.BirthDate
    };
}

public sealed class VisitVm
{
    public required DateTime Date { get; init; }
    public required string Reason { get; init; }
    public required string Notes { get; init; }

    public string WhenText => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static VisitVm FromModel(Visit v) => new()
    {
        Date = v.Date,
        Reason = v.Reason,
        Notes = v.Notes
    };
}