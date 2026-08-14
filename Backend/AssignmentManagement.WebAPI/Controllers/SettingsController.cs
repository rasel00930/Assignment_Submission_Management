using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.WebAPI.Controllers;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class SettingsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public SettingsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<GeneralResponse<IReadOnlyCollection<SettingResponse>>>> Get(
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetSettingsAsync(cancellationToken);
        return Ok(GeneralResponse<IReadOnlyCollection<SettingResponse>>.Ok(result));
    }

    [HttpPut]
    public async Task<ActionResult<GeneralResponse<SettingResponse>>> Upsert(
        SettingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.UpsertSettingAsync(request, cancellationToken);
        return Ok(GeneralResponse<SettingResponse>.Ok(result, "Setting saved."));
    }

    [HttpGet("catalog")]
    public async Task<ActionResult<GeneralResponse<IReadOnlyCollection<SettingCatalogResponse>>>> GetCatalog(
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetSettingCatalogAsync(cancellationToken);
        return Ok(GeneralResponse<IReadOnlyCollection<SettingCatalogResponse>>.Ok(result));
    }
}
