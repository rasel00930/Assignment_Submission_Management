namespace AssignmentManagement.Core.Models.Entities;

public sealed class TeacherClassSubject : BaseEntity
{
    public long TeacherId { get; set; }
    public AppUser Teacher { get; set; } = null!;

    public long AcademicClassId { get; set; }
    public AcademicClass AcademicClass { get; set; } = null!;

    public long SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public long InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;

    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
