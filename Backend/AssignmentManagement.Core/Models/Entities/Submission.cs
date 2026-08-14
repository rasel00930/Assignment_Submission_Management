using AssignmentManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace AssignmentManagement.Core.Models.Entities;

public sealed class Submission : BaseEntity
{
    public string AnswerText { get; set; } = null!;
    [MaxLength(255)]
    public string? FileName { get; set; }
    [MaxLength(500)]
    public string? StoredFilePath { get; set; }
    [MaxLength(100)]
    public string? FileContentType { get; set; }
    public long? FileSize { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public SubmissionStatus Status { get; set; }
    public decimal? Marks { get; set; }
    public string? Feedback { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    public long InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;

    public long AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    public long StudentId { get; set; }
    public AppUser Student { get; set; } = null!;

    public long? ReviewedByTeacherId { get; set; }
    public AppUser? ReviewedByTeacher { get; set; }
}
