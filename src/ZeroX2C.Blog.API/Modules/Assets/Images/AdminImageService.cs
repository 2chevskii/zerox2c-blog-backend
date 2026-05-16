using ZeroX2C.Blog.API.Modules.Assets.Images.Contracts;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Assets.Images;

public sealed class AdminImageService(BlogDbContext dbContext) : IAdminImageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
    ];

    public async Task<AdminImageOperationResult<ImageResponse>> UploadImageAsync(
        UploadImageRequest request,
        CancellationToken cancellationToken
    )
    {
        var file = request.File;
        if (file.Length == 0)
        {
            return AdminImageOperationResult<ImageResponse>.Failure(
                AdminImageOperationStatus.EmptyFile
            );
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return AdminImageOperationResult<ImageResponse>.Failure(
                AdminImageOperationStatus.FileTooLarge
            );
        }

        var contentType = file.ContentType.Trim().ToLowerInvariant();
        if (!AllowedContentTypes.Contains(contentType))
        {
            return AdminImageOperationResult<ImageResponse>.Failure(
                AdminImageOperationStatus.UnsupportedContentType
            );
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        var image = new Image
        {
            Id = Guid.CreateVersion7(),
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = contentType,
            SizeBytes = memoryStream.Length,
            Purpose = request.Purpose,
            Content = memoryStream.ToArray(),
        };

        dbContext.Images.Add(image);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminImageOperationResult<ImageResponse>.Success(ImageMapper.ToResponse(image));
    }
}
