using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.WebAPI.Controllers;

[ApiController]
[Route("api/admin/institution")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class InstitutionController : ControllerBase
{
    private readonly IAdminService _adminService;

    public InstitutionController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<GeneralResponse<InstitutionResponse>>> Get(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetInstitutionAsync(cancellationToken);
        return Ok(GeneralResponse<InstitutionResponse>.Ok(result));
    }

    [HttpPut]
    public async Task<ActionResult<GeneralResponse<InstitutionResponse>>> Update(
        UpdateInstitutionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateInstitutionAsync(request, cancellationToken);
        return Ok(GeneralResponse<InstitutionResponse>.Ok(result, "Institution configuration updated."));
    }
}
