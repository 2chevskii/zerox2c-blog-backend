using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Users.Admin.Contracts;
using ZeroX2C.Blog.API.Modules.Users.Auth;

namespace ZeroX2C.Blog.API.Modules.Users.Admin;

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

    private ActionResult<AdminUserResponse> ToActionResult(
        AdminUserOperationResult result
    ) =>
        result.Status switch
        {
            AdminUserOperationStatus.Success => result.User!,
            AdminUserOperationStatus.UserNotFound => NotFound(),
            AdminUserOperationStatus.CannotRemoveOnlySuperAdmin => BadRequest(
                new { error = "The only SuperAdmin role cannot be removed." }
            ),
            AdminUserOperationStatus.SuperAdminAlreadyExists => BadRequest(
                new { error = "A SuperAdmin user already exists." }
            ),
            AdminUserOperationStatus.CannotBlockSuperAdmin => BadRequest(
                new { error = "SuperAdmin user cannot be blocked." }
            ),
            _ => Problem(),
        };
}
