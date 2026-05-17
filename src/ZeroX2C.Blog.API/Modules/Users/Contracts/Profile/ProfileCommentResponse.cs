namespace ZeroX2C.Blog.API.Modules.Users.Contracts.Profile;

public sealed record ProfileCommentResponse(
    Guid Id,
    Guid PostId,
    string? PostSlug,
    string PostTitle,
    Guid? ParentCommentId,
    string Body,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
