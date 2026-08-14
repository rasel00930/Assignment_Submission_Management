using System.ComponentModel.DataAnnotations;
using AssignmentManagement.Core.Enums;

namespace AssignmentManagement.Core.Models.Entities;

public sealed class Assignment : BaseEntity
{
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime DeadlineUtc { get; set; }
    public decimal MaximumMarks { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;
    public bool AllowResubmission { get; set; }
    public bool AllowLateSubmission { get; set; }
    public bool AllowFileUpload { get; set; }
    public bool RequireFeedbackForGrading { get; set; }
    public bool ShowGradesImmediately { get; set; }

    public long InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;

    public long TeacherClassSubjectId { get; set; }
    public TeacherClassSubject TeacherClassSubject { get; set; } = null!;

    public long CreatedByTeacherId { get; set; }
    public AppUser CreatedByTeacher { get; set; } = null!;

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
