namespace ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;

public sealed record AdminTagResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid CreatedBy,
    DateTime CreatedAt,
    Guid? UpdatedBy,
    DateTime? UpdatedAt,
    Guid? DeletedBy,
    DateTime? DeletedAt
);
