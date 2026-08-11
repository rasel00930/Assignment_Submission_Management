using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.WebAPI.Controllers;

[ApiController]
[Route("api/teacher-assignments")]
[Authorize(Roles = AppRoles.AdminOrTeacher)]
public sealed class TeacherAssignmentsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public TeacherAssignmentsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<GeneralResponse<PagedResponse<TeacherAssignmentResponse>>>> Get(
        [FromQuery] TeacherAssignmentQueryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetTeacherAssignmentsAsync(request, cancellationToken);
        return Ok(GeneralResponse<PagedResponse<TeacherAssignmentResponse>>.Ok(result));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<GeneralResponse<TeacherAssignmentResponse>>> Create(
        AssignTeacherRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateTeacherAssignmentAsync(request, cancellationToken);
        return Ok(GeneralResponse<TeacherAssignmentResponse>.Ok(result, "Teacher assigned."));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<GeneralResponse<TeacherAssignmentResponse>>> Update(
        long id,
        UpdateTeacherAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateTeacherAssignmentAsync(id, request, cancellationToken);
        return Ok(GeneralResponse<TeacherAssignmentResponse>.Ok(result, "Teacher assignment updated."));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<GeneralResponse<object>>> Deactivate(
        long id,
        CancellationToken cancellationToken)
    {
        await _adminService.DeactivateTeacherAssignmentAsync(id, cancellationToken);
        return Ok(GeneralResponse<object>.Ok(null, "Teacher assignment deactivated."));
    }
}
