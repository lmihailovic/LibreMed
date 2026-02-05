using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreMed.Data;
using LibreMed.Models;
using LibreMed.Services;
using Microsoft.EntityFrameworkCore;

namespace LibreMed.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFileSaveDialogService _fileDialogs;

    public ObservableCollection<PatientListItemVm> Patients { get; } = new();
    public ObservableCollection<VisitVm> UpcomingVisits { get; } = new();
    public ObservableCollection<ScheduleVisitVm> UpcomingVisitsAll { get; } = new();

    public event Action<int>? RequestOpenPatientProfile;

    [ObservableProperty]
    private PatientListItemVm? selectedPatient;

    [ObservableProperty]
    private string newPatientName = string.Empty;

    [ObservableProperty]
    private string newPatientSurname = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? newPatientBirthDate = DateTimeOffset.Now;

    public bool HasSelectedPatient => SelectedPatient is not null;

    public string SelectedPatientHeader =>
        SelectedPatient is null ? "Select a patient" : SelectedPatient.FullName;

    public string SelectedPatientSubheader =>
        SelectedPatient is null ? "Choose a patient from the list to view visits." : SelectedPatient.BirthDateText;

    public MainWindowViewModel(IFileSaveDialogService fileDialogs)
    {
        _fileDialogs = fileDialogs;

        _ = RefreshAsync();
        _ = RefreshScheduleAsync();
    }

    [RelayCommand]
    private async Task ExportDbJsonAsync()
    {
        var path = await _fileDialogs.PickJsonSavePathAsync("libremed-db.json");
        if (string.IsNullOrWhiteSpace(path))
            return;

        using var db = CreateDbContext();

        var patients = await db.Patients.AsNoTracking().ToListAsync();
        var visits = await db.Visits.AsNoTracking().ToListAsync();
        var diagnoses = await db.Diagnosis.AsNoTracking().ToListAsync();

        var payload = new DbExportDto
        {
            ExportedAtUtc = DateTime.UtcNow,
            Patients = patients.Select(p => new PatientDto
            {
                Id = p.Id,
                Name = p.Name,
                Surname = p.Surname,
                BirthDate = p.BirthDate,
                PhoneNumber = p.PhoneNumber,
                Email = p.Email,
                GeneralNotes = p.GeneralNotes,
                MedicalHistory = p.MedicalHistory,
                FamilyHistory = p.FamilyHistory,
                Medications = p.Medications,
                Allergies = p.Allergies
            }).ToList(),
            Visits = visits.Select(v => new VisitDto
            {
                Id = v.Id,
                Date = v.Date,
                Notes = v.Notes,
                Reason = v.Reason,
                PatientId = v.PatientId
            }).ToList(),
            Diagnoses = diagnoses.Select(d => new DiagnosisDto
            {
                Id = d.Id,
                VisitId = d.VisitId,
                Title = d.Title,
                Details = d.Details,
                Prescription = d.Prescription,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList()
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        await File.WriteAllTextAsync(path, json);
    }

    [RelayCommand]
    private async Task ImportDbJsonAsync()
    {
        var path = await _fileDialogs.PickJsonOpenPathAsync();
        if (string.IsNullOrWhiteSpace(path))
            return;

        var json = await File.ReadAllTextAsync(path);

        var payload = JsonSerializer.Deserialize<DbExportDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (payload is null)
            return;

        using var db = CreateDbContext();
        using var tx = await db.Database.BeginTransactionAsync();

        db.Diagnosis.RemoveRange(db.Diagnosis);
        db.Visits.RemoveRange(db.Visits);
        db.Patients.RemoveRange(db.Patients);
        await db.SaveChangesAsync();

        if (payload.Patients is not null)
        {
            foreach (var p in payload.Patients)
            {
                db.Patients.Add(new Patient
                {
                    Id = p.Id,
                    Name = p.Name,
                    Surname = p.Surname,
                    BirthDate = p.BirthDate,
                    PhoneNumber = p.PhoneNumber,
                    Email = p.Email,
                    GeneralNotes = p.GeneralNotes,
                    MedicalHistory = p.MedicalHistory,
                    FamilyHistory = p.FamilyHistory,
                    Medications = p.Medications,
                    Allergies = p.Allergies
                });
            }
        }

        if (payload.Visits is not null)
        {
            foreach (var v in payload.Visits)
            {
                db.Visits.Add(new Visit
                {
                    Id = v.Id,
                    Date = v.Date,
                    Notes = v.Notes,
                    Reason = v.Reason,
                    PatientId = v.PatientId
                });
            }
        }

        if (payload.Diagnoses is not null)
        {
            foreach (var d in payload.Diagnoses)
            {
                db.Diagnosis.Add(new Diagnosis
                {
                    Id = d.Id,
                    VisitId = d.VisitId,
                    Title = d.Title,
                    Details = d.Details,
                    Prescription = d.Prescription,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                });
            }
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        await RefreshAsync();
        await RefreshScheduleAsync();
    }

    partial void OnSelectedPatientChanged(PatientListItemVm? value)
    {
        OnPropertyChanged(nameof(HasSelectedPatient));
        OnPropertyChanged(nameof(SelectedPatientHeader));
        OnPropertyChanged(nameof(SelectedPatientSubheader));
        
        _ = LoadUpcomingVisitsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
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

        if (SelectedPatient is not null)
            SelectedPatient = Patients.FirstOrDefault(x => x.Id == SelectedPatient.Id);

        // await LoadUpcomingVisitsAsync();
    }

    [RelayCommand]
    private async Task RefreshScheduleAsync()
    {
        using var db = CreateDbContext();

        var fromDate = DateTime.Today;

        var rows = await db.Visits
            .AsNoTracking()
            .Where(v => v.Date >= fromDate)
            .Join(db.Patients.AsNoTracking(),
                v => v.PatientId,
                p => p.Id,
                (v, p) => new
                {
                    v.Date,
                    v.Reason,
                    v.PatientId,
                    p.Surname,
                    p.Name
                })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Surname)
            .ThenBy(x => x.Name)
            .ToListAsync();

        UpcomingVisitsAll.Clear();
        foreach (var r in rows)
        {
            UpcomingVisitsAll.Add(new ScheduleVisitVm
            {
                Date = r.Date,
                PatientId = r.PatientId,
                PatientFullName = $"{r.Surname} {r.Name}",
                Reason = r.Reason,
                OpenProfileCommand = OpenPatientProfileFromScheduleCommand
            });
        }
    }

    [RelayCommand]
    private void OpenPatientProfileFromSchedule(int patientId)
    {
        RequestOpenPatientProfile?.Invoke(patientId);
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
            BirthDate = birthDate
        };

        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        NewPatientName = string.Empty;
        NewPatientSurname = string.Empty;

        await RefreshAsync();
        await RefreshScheduleAsync();

        SelectedPatient = Patients.FirstOrDefault(p => p.Id == patient.Id);
    }

    [RelayCommand]
    private void OpenSelectedPatientProfile()
    {
        if (SelectedPatient is null)
            return;

        RequestOpenPatientProfile?.Invoke(SelectedPatient.Id);
    }

    private async Task LoadUpcomingVisitsAsync()
    {
        UpcomingVisits.Clear();

        if (SelectedPatient is null)
            return;

        Console.WriteLine($"PatientId: {SelectedPatient.Id}");
        
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

    private sealed class DbExportDto
    {
        public DateTime ExportedAtUtc { get; set; }
        public System.Collections.Generic.List<PatientDto>? Patients { get; set; }
        public System.Collections.Generic.List<VisitDto>? Visits { get; set; }
        public System.Collections.Generic.List<DiagnosisDto>? Diagnoses { get; set; }
    }

    private sealed class PatientDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public DateTime BirthDate { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? GeneralNotes { get; set; }
        public string? MedicalHistory { get; set; }
        public string? FamilyHistory { get; set; }
        public System.Collections.Generic.List<string>? Medications { get; set; }
        public System.Collections.Generic.List<string>? Allergies { get; set; }
    }

    private sealed class VisitDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public required string Notes { get; set; }
        public required string Reason { get; set; }
        public int PatientId { get; set; }
    }

    private sealed class DiagnosisDto
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public required string Title { get; set; }
        public string? Details { get; set; }
        public string? Prescription { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
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
    public required int Id { get; init; }
    public required DateTime Date { get; init; }
    public required string Reason { get; init; }
    public required string Notes { get; init; }

    public string WhenText => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static VisitVm FromModel(Visit v) => new()
    {
        Id = v.Id,
        Date = v.Date,
        Reason = v.Reason,
        Notes = v.Notes
    };
}

public sealed class ScheduleVisitVm
{
    public required DateTime Date { get; init; }
    public required int PatientId { get; init; }
    public required string PatientFullName { get; init; }
    public required string Reason { get; init; }

    public required IRelayCommand<int> OpenProfileCommand { get; init; }

    public string WhenText => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}