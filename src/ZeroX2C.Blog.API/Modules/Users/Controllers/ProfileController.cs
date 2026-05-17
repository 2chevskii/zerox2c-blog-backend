using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Modules.Users.Contracts.Profile;
using ZeroX2C.Blog.API.Modules.Users.Profile;

namespace ZeroX2C.Blog.API.Modules.Users.Controllers;

[ApiController, Route("api/profile")]
[Authorize]
public sealed class ProfileController(IProfileService profileService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> GetProfile(
        CancellationToken cancellationToken
    )
    {
        var profile = await profileService.GetProfileAsync(cancellationToken);
        return profile is null ? Unauthorized() : profile;
    }

    [HttpPost("avatar")]
    [Authorize(Policy = AuthorizationPolicyNames.AuthenticatedNotBlocked)]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<ProfileResponse>> UploadAvatar(
        [FromForm] UploadAvatarRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await profileService.UploadAvatarAsync(request, cancellationToken);
        return result.Status switch
        {
            ProfileOperationStatus.Success => result.Value!,
            ProfileOperationStatus.UserNotFound => Unauthorized(),
            ProfileOperationStatus.EmptyFile => ValidationProblem("Avatar image file is empty."),
            ProfileOperationStatus.FileTooLarge => ValidationProblem(
                "Avatar image file must not exceed 2 MB."
            ),
            ProfileOperationStatus.UnsupportedContentType => ValidationProblem(
                "Avatar image file must be JPEG, PNG, or WebP."
            ),
            _ => Problem(),
        };
    }

    [HttpPut("comment-replies/{replyCommentId:guid}/seen")]
    [Authorize(Policy = AuthorizationPolicyNames.AuthenticatedNotBlocked)]
    public async Task<ActionResult<ProfileCommentReplyResponse>> MarkReplySeen(
        Guid replyCommentId,
        CancellationToken cancellationToken
    )
    {
        var result = await profileService.MarkReplySeenAsync(replyCommentId, cancellationToken);
        return result.Status switch
        {
            ProfileOperationStatus.Success => result.Value!,
            ProfileOperationStatus.ReplyNotFound => NotFound(),
            _ => Problem(),
        };
    }
}
