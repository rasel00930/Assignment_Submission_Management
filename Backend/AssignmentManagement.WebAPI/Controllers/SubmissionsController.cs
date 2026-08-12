using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.WebAPI.Controllers;

[ApiController]
[Route("api/submissions")]
[Authorize]
public sealed class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;

    public SubmissionsController(ISubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    [HttpGet]
    public async Task<ActionResult<GeneralResponse<PagedResponse<SubmissionResponse>>>> Get(
        [FromQuery] SubmissionQueryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _submissionService.GetAsync(request, cancellationToken);
        return Ok(GeneralResponse<PagedResponse<SubmissionResponse>>.Ok(result));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<GeneralResponse<SubmissionResponse>>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _submissionService.GetByIdAsync(id, cancellationToken);
        return Ok(GeneralResponse<SubmissionResponse>.Ok(result));
    }

    [Authorize(Roles = AppRoles.Student)]
    [HttpPost("assignment/{assignmentId:long}")]
    public async Task<ActionResult<GeneralResponse<SubmissionResponse>>> Submit(
        long assignmentId,
        SubmitAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _submissionService.SubmitAsync(assignmentId, request, cancellationToken);
        return Ok(GeneralResponse<SubmissionResponse>.Ok(result, "Submission saved."));
    }

    [Authorize(Roles = AppRoles.Teacher)]
    [HttpPut("{id:long}/review")]
    public async Task<ActionResult<GeneralResponse<SubmissionResponse>>> Review(
        long id,
        ReviewSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _submissionService.ReviewAsync(id, request, cancellationToken);
        return Ok(GeneralResponse<SubmissionResponse>.Ok(result, "Submission reviewed."));
    }
}
