using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.WebAPI.Controllers;

[ApiController]
[Route("api/admin/classes")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class ClassesController : ControllerBase
{
    private readonly IAdminService _adminService;

    public ClassesController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<GeneralResponse<IReadOnlyCollection<ClassResponse>>>> Get(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetClassesAsync(includeInactive, cancellationToken);
        return Ok(GeneralResponse<IReadOnlyCollection<ClassResponse>>.Ok(result));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<GeneralResponse<ClassResponse>>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetClassByIdAsync(id, cancellationToken);
        return Ok(GeneralResponse<ClassResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<GeneralResponse<ClassResponse>>> Create(
        CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateClassAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            GeneralResponse<ClassResponse>.Ok(result, "Class/course created."));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<GeneralResponse<ClassResponse>>> Update(
        long id,
        UpdateClassRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateClassAsync(id, request, cancellationToken);
        return Ok(GeneralResponse<ClassResponse>.Ok(result, "Class/course updated."));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<GeneralResponse<object>>> Deactivate(
        long id,
        CancellationToken cancellationToken)
    {
        await _adminService.DeactivateClassAsync(id, cancellationToken);
        return Ok(GeneralResponse<object>.Ok(null, "Class/course deactivated."));
    }
}
