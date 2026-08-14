using System.ComponentModel.DataAnnotations;
using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Common;

namespace AssignmentManagement.Core.Models.DTO;

public sealed class UserQueryRequest : PagingRequest
{
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public long? AcademicClassId { get; set; }
}

public  class CreateUserRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = null!;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = null!;

    [Required, MaxLength(100)]
    public string UserName { get; set; } = null!;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = null!;

    [Required]
    public string Role { get; set; } = null!;

    public long? AcademicClassId { get; set; }
}

public  class UpdateUserRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = null!;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = null!;

    [Required, MaxLength(100)]
    public string UserName { get; set; } = null!;

    [Required]
    public string Role { get; set; } = null!;

    public long? AcademicClassId { get; set; }
}

public  class ResetPasswordRequest
{
    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; set; } = null!;
}

public  record UserResponse(
    long Id,
    string FullName,
    string Email,
    string UserName,
    bool IsActive,
    IReadOnlyCollection<string> Roles,
    long? AcademicClassId,
    string? AcademicClassName,
    DateTime CreatedAtUtc);

public  class UpdateInstitutionRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(30)]
    public string Code { get; set; } = null!;

    [EnumDataType(typeof(InstitutionType))]
    public InstitutionType Type { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [EmailAddress, MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? LogoUrl { get; set; }
}

public  record InstitutionResponse(
    long Id,
    string Name,
    string Code,
    InstitutionType Type,
    string? Address,
    string? Email,
    string? Phone,
    string? LogoUrl);

public  class CreateClassRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(50)]
    public string? Section { get; set; }

    [Required, MaxLength(30)]
    public string AcademicYear { get; set; } = null!;
}

public  class UpdateClassRequest : CreateClassRequest
{
    public bool IsActive { get; set; } = true;
}

public sealed record ClassResponse(
    long Id,
    string Name,
    string? Section,
    string AcademicYear,
    bool IsActive,
    int StudentCount);

public  class CreateSubjectRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(30)]
    public string Code { get; set; } = null!;
}

public sealed class UpdateSubjectRequest : CreateSubjectRequest
{
    public bool IsActive { get; set; } = true;
}

public sealed record SubjectResponse(long Id, string Name, string Code, bool IsActive);

public sealed class TeacherAssignmentQueryRequest : PagingRequest
{
    public long? TeacherId { get; set; }
    public long? AcademicClassId { get; set; }
    public long? SubjectId { get; set; }
    public bool? IsActive { get; set; }
}

public  class AssignTeacherRequest
{
    [Range(1, long.MaxValue)]
    public long TeacherId { get; set; }

    [Range(1, long.MaxValue)]
    public long AcademicClassId { get; set; }

    [Range(1, long.MaxValue)]
    public long SubjectId { get; set; }
}

public sealed class UpdateTeacherAssignmentRequest : AssignTeacherRequest
{
    public bool IsActive { get; set; } = true;
}

public sealed record TeacherAssignmentResponse(
    long Id,
    long TeacherId,
    string TeacherName,
    long AcademicClassId,
    string ClassName,
    long SubjectId,
    string SubjectName,
    bool IsActive);

public sealed class SettingRequest
{
    [Required, MaxLength(100)]
    public string Key { get; set; } = null!;

    [Required]
    public string Value { get; set; } = null!;

    [MaxLength(300)]
    public string? Description { get; set; }
}

public sealed record SettingResponse(long Id, string Key, string Value, string? Description);

public sealed record SettingCatalogResponse(
    string Key,
    string Title,
    string Description,
    string Alignment,
    bool DefaultValue,
    bool IsConfigured,
    bool IsEnabled);
