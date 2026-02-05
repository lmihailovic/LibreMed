using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreMed.Data;
using LibreMed.Models;
using LibreMed.Services;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace LibreMed.ViewModels;

public partial class DiagnosisWindowViewModel : ViewModelBase
{
    private readonly int _visitId;
    private readonly IFileSaveDialogService _fileSaveDialog;

    [ObservableProperty] private string windowTitle = "Diagnosis";
    [ObservableProperty] private string header = "Diagnosis";
    [ObservableProperty] private string subheader = "";

    [ObservableProperty] private string titleText = string.Empty;
    [ObservableProperty] private string detailsText = string.Empty;
    [ObservableProperty] private string prescriptionText = string.Empty;

    public DiagnosisWindowViewModel(int visitId, IFileSaveDialogService fileSaveDialog)
    {
        _visitId = visitId;
        _fileSaveDialog = fileSaveDialog;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        var path = await _fileSaveDialog.PickPdfSavePathAsync($"diagnosis-visit-{_visitId}.pdf");
        if (string.IsNullOrWhiteSpace(path))
            return;

        await ExportDiagnosisPdfAsync(path);
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

    private async Task ExportDiagnosisPdfAsync(string outputPath)
    {
        using var db = CreateDbContext();

        var visit = await db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Diagnosis)
            .FirstAsync(v => v.Id == _visitId);

        using var doc = new PdfDocument();
        doc.Info.Title = $"Diagnosis Report - Visit {_visitId}";

        var page = doc.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;

        using var gfx = XGraphics.FromPdfPage(page);

        // Use a built-in PDF font name to avoid Linux font resolver issues.
        var titleFont = new XFont("Helvetica", 18, XFontStyle.Bold);
        var hFont = new XFont("Helvetica", 12, XFontStyle.Bold);
        var font = new XFont("Helvetica", 11, XFontStyle.Regular);

        double x = 40;
        double y = 40;
        double line = 16;
        double maxWidth = page.Width - 2 * x;

        void Write(string text, XFont f)
        {
            foreach (var part in WrapApprox(text, 100))
            {
                gfx.DrawString(part, f, XBrushes.Black, new XRect(x, y, maxWidth, line), XStringFormats.TopLeft);
                y += line;
            }
        }

        Write("Diagnosis Report", titleFont);
        y += 8;

        var patientName = $"{visit.Patient?.Surname} {visit.Patient?.Name}".Trim();
        if (!string.IsNullOrWhiteSpace(patientName))
        {
            Write("Patient", hFont);
            Write($"Name: {patientName}", font);
            y += 6;
        }

        Write("Visit", hFont);
        Write($"Visit ID: {visit.Id}", font);
        Write($"Date: {visit.Date:yyyy-MM-dd}", font);
        Write($"Reason: {visit.Reason}", font);
        Write($"Notes: {visit.Notes}", font);
        y += 8;

        Write("Diagnosis", hFont);
        if (visit.Diagnosis is null)
        {
            Write("No diagnosis recorded for this visit.", font);
        }
        else
        {
            Write($"Title: {visit.Diagnosis.Title}", font);
            Write($"Prescription: {visit.Diagnosis.Prescription ?? "—"}", font);
            Write($"Details: {visit.Diagnosis.Details ?? "—"}", font);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        doc.Save(outputPath);
    }

    private static string? NormalizeOptional(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }

    private static System.Collections.Generic.IEnumerable<string> WrapApprox(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return "—";
            yield break;
        }

        var t = text.Trim();
        while (t.Length > maxChars)
        {
            var cut = t.LastIndexOf(' ', maxChars);
            if (cut <= 0) cut = maxChars;
            yield return t[..cut].TrimEnd();
            t = t[cut..].TrimStart();
        }

        yield return t;
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