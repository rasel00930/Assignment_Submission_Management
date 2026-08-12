using System.ComponentModel.DataAnnotations;

namespace AssignmentManagement.Core.Models.Entities;

public sealed class ApplicationSetting : BaseEntity
{
    [MaxLength(100)]
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    [MaxLength(300)]
    public string? Description { get; set; }

    public long InstitutionId { get; set; }
    public Institution Institution { get; set; } = null!;
}
