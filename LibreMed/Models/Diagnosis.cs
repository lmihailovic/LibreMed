using System;

namespace LibreMed.Models;

public class Diagnosis
{
    public int Id { get; set; }

    public required string Title { get; set; }
    public string? Details { get; set; }

    public string? Prescription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int VisitId { get; set; }
    public Visit? Visit { get; set; }
}