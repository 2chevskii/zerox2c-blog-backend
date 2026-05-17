using ZeroX2C.Blog.API.Modules.Users.Contracts.Profile;

namespace ZeroX2C.Blog.API.Modules.Users.Profile;

public interface IProfileService
{
    Task<ProfileResponse?> GetProfileAsync(CancellationToken cancellationToken);

    Task<ProfileOperationResult<ProfileResponse>> UploadAvatarAsync(
        UploadAvatarRequest request,
        CancellationToken cancellationToken
    );

    Task<ProfileOperationResult<ProfileCommentReplyResponse>> MarkReplySeenAsync(
        Guid replyCommentId,
        CancellationToken cancellationToken
    );
}
