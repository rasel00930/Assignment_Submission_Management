using System.ComponentModel.DataAnnotations;

namespace AssignmentManagement.Core.Models.Entities;

public sealed class RefreshToken : BaseEntity
{
    [MaxLength(128)]
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    [MaxLength(128)]
    public string? ReplacedByTokenHash { get; set; }

    public long UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public bool IsUsable(DateTime nowUtc) => IsActive && RevokedAtUtc is null && ExpiresAtUtc > nowUtc;
}
