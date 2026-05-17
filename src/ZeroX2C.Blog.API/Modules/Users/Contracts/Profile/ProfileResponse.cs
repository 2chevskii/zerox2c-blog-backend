using ZeroX2C.Blog.API.Modules.Posts.Contracts;

namespace ZeroX2C.Blog.API.Modules.Users.Contracts.Profile;

public sealed record ProfileResponse(
    Guid UserId,
    string Username,
    string Email,
    Guid? AvatarImageId,
    IReadOnlyCollection<PostListItemResponse> RecentlyViewedPosts,
    IReadOnlyCollection<PostListItemResponse> LikedPosts,
    IReadOnlyCollection<ProfileCommentResponse> Comments,
    IReadOnlyCollection<ProfileCommentReplyResponse> Replies
);
