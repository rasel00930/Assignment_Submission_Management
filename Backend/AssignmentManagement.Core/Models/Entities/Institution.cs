using System.ComponentModel.DataAnnotations;
using AssignmentManagement.Core.Enums;

namespace AssignmentManagement.Core.Models.Entities;

public sealed class Institution : BaseEntity
{
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(30)]
    public string Code { get; set; } = null!;

    public InstitutionType Type { get; set; } = InstitutionType.School;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? LogoUrl { get; set; }

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<AcademicClass> AcademicClasses { get; set; } = new List<AcademicClass>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<ApplicationSetting> Settings { get; set; } = new List<ApplicationSetting>();
}
