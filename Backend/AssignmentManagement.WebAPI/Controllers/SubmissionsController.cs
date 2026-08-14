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
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024 + 64 * 1024)]
    public async Task<ActionResult<GeneralResponse<SubmissionResponse>>> Submit(
        long assignmentId,
        [FromForm] SubmitAssignmentRequest request,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        SubmissionResponse result;
        if (file is null)
        {
            result = await _submissionService.SubmitAsync(
                assignmentId,
                request,
                file: null,
                cancellationToken);
        }
        else
        {
            await using var content = file.OpenReadStream();
            var upload = new SubmissionFileUpload(
                content,
                file.FileName,
                file.ContentType,
                file.Length);
            result = await _submissionService.SubmitAsync(
                assignmentId,
                request,
                upload,
                cancellationToken);
        }

        return Ok(GeneralResponse<SubmissionResponse>.Ok(result, "Submission saved."));
    }

    [HttpGet("{id:long}/file")]
    public async Task<IActionResult> DownloadFile(
        long id,
        [FromQuery] bool download = false,
        CancellationToken cancellationToken = default)
    {
        var file = await _submissionService.DownloadFileAsync(id, cancellationToken);
        if (download)
            return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);

        Response.Headers.ContentDisposition = $"inline; filename=\"{Uri.EscapeDataString(file.FileName)}\"";
        return File(file.Content, file.ContentType, enableRangeProcessing: true);
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
