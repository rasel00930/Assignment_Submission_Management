using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.WebAPI.Controllers;

[ApiController]
[Route("api/admin/subjects")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class SubjectsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public SubjectsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<GeneralResponse<IReadOnlyCollection<SubjectResponse>>>> Get(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetSubjectsAsync(includeInactive, cancellationToken);
        return Ok(GeneralResponse<IReadOnlyCollection<SubjectResponse>>.Ok(result));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<GeneralResponse<SubjectResponse>>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetSubjectByIdAsync(id, cancellationToken);
        return Ok(GeneralResponse<SubjectResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<GeneralResponse<SubjectResponse>>> Create(
        CreateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateSubjectAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            GeneralResponse<SubjectResponse>.Ok(result, "Subject created."));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<GeneralResponse<SubjectResponse>>> Update(
        long id,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateSubjectAsync(id, request, cancellationToken);
        return Ok(GeneralResponse<SubjectResponse>.Ok(result, "Subject updated."));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<GeneralResponse<object>>> Deactivate(
        long id,
        CancellationToken cancellationToken)
    {
        await _adminService.DeactivateSubjectAsync(id, cancellationToken);
        return Ok(GeneralResponse<object>.Ok(null, "Subject deactivated."));
    }
}
