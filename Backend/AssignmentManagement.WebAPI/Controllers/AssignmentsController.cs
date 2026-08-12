using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.WebAPI.Controllers;

[ApiController]
[Route("api/assignments")]
[Authorize]
public sealed class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;

    public AssignmentsController(IAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<ActionResult<GeneralResponse<PagedResponse<AssignmentResponse>>>> Get(
        [FromQuery] AssignmentQueryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetAsync(request, cancellationToken);
        return Ok(GeneralResponse<PagedResponse<AssignmentResponse>>.Ok(result));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<GeneralResponse<AssignmentResponse>>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetByIdAsync(id, cancellationToken);
        return Ok(GeneralResponse<AssignmentResponse>.Ok(result));
    }

    [Authorize(Roles = AppRoles.Teacher)]
    [HttpPost]
    public async Task<ActionResult<GeneralResponse<AssignmentResponse>>> Create(
        CreateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            GeneralResponse<AssignmentResponse>.Ok(result, "Assignment created."));
    }

    [Authorize(Roles = AppRoles.Teacher)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<GeneralResponse<AssignmentResponse>>> Update(
        long id,
        UpdateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.UpdateAsync(id, request, cancellationToken);
        return Ok(GeneralResponse<AssignmentResponse>.Ok(result, "Assignment updated."));
    }

    [Authorize(Roles = AppRoles.Teacher)]
    [HttpPost("{id:long}/publish")]
    public async Task<ActionResult<GeneralResponse<object>>> Publish(
        long id,
        CancellationToken cancellationToken)
    {
        await _assignmentService.PublishAsync(id, cancellationToken);
        return Ok(GeneralResponse<object>.Ok(null, "Assignment published."));
    }

    [Authorize(Roles = AppRoles.Teacher)]
    [HttpPost("{id:long}/draft")]
    public async Task<ActionResult<GeneralResponse<object>>> MoveToDraft(
        long id,
        CancellationToken cancellationToken)
    {
        await _assignmentService.MoveToDraftAsync(id, cancellationToken);
        return Ok(GeneralResponse<object>.Ok(null, "Assignment moved to draft."));
    }

    [Authorize(Roles = AppRoles.Teacher)]
    [HttpPost("{id:long}/close")]
    public async Task<ActionResult<GeneralResponse<object>>> Close(
        long id,
        CancellationToken cancellationToken)
    {
        await _assignmentService.CloseAsync(id, cancellationToken);
        return Ok(GeneralResponse<object>.Ok(null, "Assignment closed."));
    }

    [Authorize(Roles = AppRoles.Teacher)]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<GeneralResponse<object>>> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        await _assignmentService.DeleteAsync(id, cancellationToken);
        return Ok(GeneralResponse<object>.Ok(null, "Assignment deleted."));
    }
}
