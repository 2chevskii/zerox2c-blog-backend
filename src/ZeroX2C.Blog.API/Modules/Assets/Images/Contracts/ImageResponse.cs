using ZeroX2C.Blog.API.Modules.Assets.Images;

namespace ZeroX2C.Blog.API.Modules.Assets.Images.Contracts;

public sealed record ImageResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    ImagePurpose Purpose,
    string Url,
    Guid CreatedBy,
    DateTime CreatedAt
);
