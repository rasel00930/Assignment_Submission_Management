namespace AssignmentManagement.Core.Models.Entities;

public sealed class UserRole
{
    public long UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public long RoleId { get; set; }
    public AppRole Role { get; set; } = null!;
}
