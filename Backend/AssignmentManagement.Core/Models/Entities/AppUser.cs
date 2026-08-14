using System.ComponentModel.DataAnnotations;

namespace AssignmentManagement.Core.Models.Entities;

public sealed class AppUser : BaseEntity
{
    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    [MaxLength(100)]
    public string Email { get; set; } = null!;

    [MaxLength(100)]
    public string UserName { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public long InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;

    public long? AcademicClassId { get; set; }
    public AcademicClass? AcademicClass { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetCode> PasswordResetCodes { get; set; } = new List<PasswordResetCode>();
}
