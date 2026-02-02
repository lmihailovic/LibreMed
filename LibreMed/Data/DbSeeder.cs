using System;
using System.Linq;
using LibreMed.Models;

namespace LibreMed.Data;

public static class DbSeeder
{
    public static void SeedIfEmpty(AppDbContext db)
    {
        if (db.Patients.Any())
            return;

        var p1 = new Patient
        {
            Name = "Alex",
            Surname = "Doe",
            BirthDate = new DateTime(1990, 5, 12),
            PhoneNumber = null,
            Email = null,
            GeneralNotes = "Seed patient (safe to delete).",
            MedicalHistory = "No significant history reported.",
            FamilyHistory = null,
            Medications = ["Vitamin D"],
            Allergies = ["Penicillin"]
        };

        var p2 = new Patient
        {
            Name = "Sam",
            Surname = "Roe",
            BirthDate = new DateTime(1985, 11, 3),
            PhoneNumber = null,
            Email = null,
            GeneralNotes = null,
            MedicalHistory = null,
            FamilyHistory = null,
            Medications = null,
            Allergies = null
        };

        db.Patients.AddRange(p1, p2);

        db.Visits.Add(new Visit
        {
            Date = DateTime.Today,
            Reason = "Routine check",
            Notes = "All vitals normal.",
            Prescription = "None",
            Patient = p1
        });

        db.SaveChanges();
    }
}