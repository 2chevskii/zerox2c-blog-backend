using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Users.Admin;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Modules.Users.Contracts.Admin;

namespace ZeroX2C.Blog.API.Modules.Users.Controllers;

[ApiController, Route("api/admin/users")]
[Authorize(Policy = AuthorizationPolicyNames.Admin)]
public sealed class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminUserResponse>>> GetUsers(
        CancellationToken cancellationToken
    ) =>
        Ok(await adminUserService.GetUsersAsync(cancellationToken));

    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = AuthorizationPolicyNames.SuperAdmin)]
    public async Task<ActionResult<AdminUserResponse>> UpdateRole(
        Guid id,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken
    ) =>
        ToActionResult(
            await adminUserService.UpdateRoleAsync(id, request.Role, cancellationToken)
        );

    [HttpPost("{id:guid}/block")]
    public async Task<ActionResult<AdminUserResponse>> BlockUser(
        Guid id,
        BlockUserRequest request,
        CancellationToken cancellationToken
    ) =>
        ToActionResult(
            await adminUserService.BlockUserAsync(id, request.Reason, cancellationToken)
        );

    [HttpPost("{id:guid}/unblock")]
    public async Task<ActionResult<AdminUserResponse>> UnblockUser(
        Guid id,
        CancellationToken cancellationToken
    ) =>
        ToActionResult(
            await adminUserService.UnblockUserAsync(id, cancellationToken)
        );

    [HttpPut("{id:guid}/password")]
    [Authorize(Policy = AuthorizationPolicyNames.SuperAdmin)]
    public async Task<ActionResult<AdminUserResponse>> UpdatePassword(
        Guid id,
        UpdateUserPasswordRequest request,
        CancellationToken cancellationToken
    ) =>
        ToActionResult(
            await adminUserService.UpdatePasswordAsync(id, request.Password, cancellationToken)
        );

    private ActionResult<AdminUserResponse> ToActionResult(
        AdminUserOperationResult result
    ) =>
        result.Status switch
        {
            AdminUserOperationStatus.Success => result.User!,
            AdminUserOperationStatus.UserNotFound => NotFound(),
            AdminUserOperationStatus.SuperAdminAlreadyExists => BadRequest(
                new { error = "Only known technical users can hold SuperAdmin role." }
            ),
            AdminUserOperationStatus.CannotBlockSuperAdmin => BadRequest(
                new { error = "SuperAdmin user cannot be blocked." }
            ),
            AdminUserOperationStatus.KnownUserCannotBeModified => BadRequest(
                new { error = "Known technical users cannot be modified." }
            ),
            AdminUserOperationStatus.PasswordCannotBeChanged => BadRequest(
                new { error = "Password can only be changed for the known superadmin user." }
            ),
            _ => Problem(),
        };
}
