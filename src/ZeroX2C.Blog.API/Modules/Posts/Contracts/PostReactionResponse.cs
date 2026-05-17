using ZeroX2C.Blog.API.Modules.Posts;

namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record PostReactionResponse(
    Guid PostId,
    long LikeCount,
    long DislikeCount,
    PostReactionType? CurrentUserReaction
);
