using System.ComponentModel.DataAnnotations;

namespace AssignmentManagement.Core.Models.Common;

public class PagingRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Search { get; set; }
}
