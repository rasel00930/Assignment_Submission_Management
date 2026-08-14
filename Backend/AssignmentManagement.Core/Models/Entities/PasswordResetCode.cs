using System.ComponentModel.DataAnnotations;

namespace AssignmentManagement.Core.Models.Entities;

public sealed class PasswordResetCode : BaseEntity
{
    [MaxLength(128)]
    public string CodeHash { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public int FailedAttempts { get; set; }

    public long UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public bool IsUsable(DateTime nowUtc, int maxFailedAttempts) =>
        IsActive &&
        UsedAtUtc is null &&
        ExpiresAtUtc > nowUtc &&
        FailedAttempts < maxFailedAttempts;
}
