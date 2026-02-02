using System;
using System.Collections.Generic;

namespace LibreMed.Models;

public class Patient
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

    public List<string>? Medications { get; set; }
    public List<string>? Allergies { get; set; }

    public List<Visit> Visits { get; set; } = new();
}