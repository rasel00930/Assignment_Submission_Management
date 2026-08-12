using System.ComponentModel.DataAnnotations;

namespace AssignmentManagement.Core.Models.Entities;

public sealed class Subject : BaseEntity
{
    [MaxLength(120)]
    public string Name { get; set; } = null!;

    [MaxLength(30)]
    public string Code { get; set; } = null!;

    public long InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;

    public ICollection<TeacherClassSubject> TeacherClassSubjects { get; set; } = new List<TeacherClassSubject>();
}
