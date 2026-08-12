using System.ComponentModel.DataAnnotations;
using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Common;

namespace AssignmentManagement.Core.Models.DTO;

public sealed class AssignmentQueryRequest : PagingRequest
{
    public AssignmentStatus? Status { get; set; }
    public long? AcademicClassId { get; set; }
    public long? SubjectId { get; set; }
    public long? TeacherId { get; set; }
}

public sealed class CreateAssignmentRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required, MinLength(3)]
    public string Description { get; set; } = null!;

    public DateTime DeadlineUtc { get; set; }

    [Range(typeof(decimal), "0.01", "10000")]
    public decimal MaximumMarks { get; set; }

    [Range(1, long.MaxValue)]
    public long TeacherClassSubjectId { get; set; }

    public bool AllowResubmission { get; set; } = true;
    public bool PublishNow { get; set; }
}

public sealed class UpdateAssignmentRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required, MinLength(3)]
    public string Description { get; set; } = null!;

    public DateTime DeadlineUtc { get; set; }

    [Range(typeof(decimal), "0.01", "10000")]
    public decimal MaximumMarks { get; set; }

    [Range(1, long.MaxValue)]
    public long TeacherClassSubjectId { get; set; }

    public bool AllowResubmission { get; set; } = true;
}

public sealed record AssignmentResponse(
    long Id,
    string Title,
    string Description,
    DateTime DeadlineUtc,
    decimal MaximumMarks,
    AssignmentStatus Status,
    bool AllowResubmission,
    long TeacherClassSubjectId,
    long AcademicClassId,
    string ClassName,
    string? Section,
    long SubjectId,
    string SubjectName,
    long TeacherId,
    string TeacherName,
    int SubmissionCount,
    DateTime CreatedAtUtc);
