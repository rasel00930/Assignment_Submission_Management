using System.ComponentModel.DataAnnotations;

namespace AssignmentManagement.Core.Models.DTO;

public sealed class LoginRequest
{
    [Required, MaxLength(100)]
    public string UserName { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; set; } = null!;
}

public sealed record UserSummary(
    long Id,
    string FullName,
    string UserName,
    string Email,
    long InstitutionId,
    string InstitutionName,
    IReadOnlyCollection<string> Roles,
    long? AcademicClassId,
    string? AcademicClassName);

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserSummary User);
