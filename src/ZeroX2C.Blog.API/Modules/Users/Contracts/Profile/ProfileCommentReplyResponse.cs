namespace ZeroX2C.Blog.API.Modules.Users.Contracts.Profile;

public sealed record ProfileCommentReplyResponse(
    Guid Id,
    Guid PostId,
    string? PostSlug,
    string PostTitle,
    Guid ParentCommentId,
    string ParentCommentBody,
    Guid AuthorUserId,
    string AuthorUsername,
    string Body,
    DateTime CreatedAt,
    bool IsSeen,
    DateTime? SeenAt
);
