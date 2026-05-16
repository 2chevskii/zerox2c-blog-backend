namespace ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;

public sealed record TagResponse(
    Guid Id,
    string Name,
    string? Description
);
