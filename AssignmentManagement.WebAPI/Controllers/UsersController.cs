using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.WebAPI.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class UsersController : ControllerBase
{
    private readonly IAdminService _adminService;

    public UsersController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<GeneralResponse<PagedResponse<UserResponse>>>> Get(
        [FromQuery] UserQueryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetUsersAsync(request, cancellationToken);
        return Ok(GeneralResponse<PagedResponse<UserResponse>>.Ok(result));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<GeneralResponse<UserResponse>>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetUserByIdAsync(id, cancellationToken);
        return Ok(GeneralResponse<UserResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<GeneralResponse<UserResponse>>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            GeneralResponse<UserResponse>.Ok(result, "User created."));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<GeneralResponse<UserResponse>>> Update(
        long id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateUserAsync(id, request, cancellationToken);
        return Ok(GeneralResponse<UserResponse>.Ok(result, "User updated."));
    }

    [HttpPatch("{id:long}/active")]
    public async Task<ActionResult<GeneralResponse<object>>> SetActive(
        long id,
        [FromQuery] bool value,
        CancellationToken cancellationToken)
    {
        await _adminService.SetUserActiveAsync(id, value, cancellationToken);
        return Ok(GeneralResponse<object>.Ok(null, "User status updated."));
    }

    [HttpPost("{id:long}/reset-password")]
    public async Task<ActionResult<GeneralResponse<object>>> ResetPassword(
        long id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _adminService.ResetPasswordAsync(id, request, cancellationToken);
        return Ok(GeneralResponse<object>.Ok(null, "Password reset."));
    }
}
