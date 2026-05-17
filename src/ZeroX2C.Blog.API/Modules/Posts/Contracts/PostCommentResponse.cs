namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record PostCommentResponse(
    Guid Id,
    Guid PostId,
    Guid AuthorUserId,
    string AuthorUsername,
    Guid? ParentCommentId,
    string Body,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
