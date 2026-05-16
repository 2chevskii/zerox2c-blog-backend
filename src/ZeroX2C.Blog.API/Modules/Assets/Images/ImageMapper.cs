using ZeroX2C.Blog.API.Modules.Assets.Images.Contracts;

namespace ZeroX2C.Blog.API.Modules.Assets.Images;

public static class ImageMapper
{
    public static ImageResponse ToResponse(Image image) =>
        new(
            image.Id,
            image.OriginalFileName,
            image.ContentType,
            image.SizeBytes,
            image.Purpose,
            $"/api/images/{image.Id}",
            image.CreatedBy,
            image.CreatedAt
        );
}
