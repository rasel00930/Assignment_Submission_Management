using System.ComponentModel.DataAnnotations;

namespace AssignmentManagement.Core.Models.Entities;

public sealed class AcademicClass : BaseEntity
{
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(50)]
    public string? Section { get; set; }

    [MaxLength(30)]
    public string AcademicYear { get; set; } = null!;

    public long InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;

    public ICollection<AppUser> Students { get; set; } = new List<AppUser>();
    public ICollection<TeacherClassSubject> TeacherClassSubjects { get; set; } = new List<TeacherClassSubject>();
}
