namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record PostMarkdownImageResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Url,
    string LocalPath,
    DateTime CreatedAt
);
