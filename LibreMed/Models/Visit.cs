using System;

namespace LibreMed.Models;

public class Visit
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public required string Notes { get; set; }
    public required string Reason { get; set; }
    public required string Prescription { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
}