using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Assets.Images;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Posts.Markdown;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed class AdminPostMarkdownImageService(BlogDbContext dbContext)
    : IAdminPostMarkdownImageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
    ];

    public async Task<AdminPostOperationResult<IReadOnlyCollection<PostMarkdownImageResponse>>> GetImagesAsync(
        Guid postId,
        CancellationToken cancellationToken
    )
    {
        var postExists = await dbContext.Posts.AnyAsync(
            post => post.Id == postId && !post.IsDeleted,
            cancellationToken
        );
        if (!postExists)
        {
            return AdminPostOperationResult<IReadOnlyCollection<PostMarkdownImageResponse>>.Failure(
                AdminPostOperationStatus.PostNotFound
            );
        }

        var images = await dbContext.PostMarkdownImages
            .Include(image => image.Image)
            .Where(image =>
                image.PostId == postId
                && !image.IsDeleted
                && !image.Image.IsDeleted
            )
            .OrderByDescending(image => image.CreatedAt)
            .ThenByDescending(image => image.Id)
            .ToArrayAsync(cancellationToken);

        return AdminPostOperationResult<IReadOnlyCollection<PostMarkdownImageResponse>>.Success(
            images.Select(MarkdownDocumentMapper.ToResponse).ToArray()
        );
    }

    public async Task<AdminPostOperationResult<PostMarkdownImageResponse>> UploadImageAsync(
        Guid postId,
        UploadPostMarkdownImageRequest request,
        CancellationToken cancellationToken
    )
    {
        var postExists = await dbContext.Posts.AnyAsync(
            post => post.Id == postId && !post.IsDeleted,
            cancellationToken
        );
        if (!postExists)
        {
            return AdminPostOperationResult<PostMarkdownImageResponse>.Failure(
                AdminPostOperationStatus.PostNotFound
            );
        }

        var validationStatus = ValidateImage(request.File);
        if (validationStatus != AdminPostOperationStatus.Success)
        {
            return AdminPostOperationResult<PostMarkdownImageResponse>.Failure(validationStatus);
        }

        await using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);

        var imageId = Guid.CreateVersion7();
        var image = new Image
        {
            Id = imageId,
            OriginalFileName = Path.GetFileName(request.File.FileName),
            ContentType = request.File.ContentType.Trim().ToLowerInvariant(),
            SizeBytes = memoryStream.Length,
            Purpose = ImagePurpose.Embedded,
            Content = memoryStream.ToArray(),
        };

        var postMarkdownImage = new PostMarkdownImage
        {
            Id = Guid.CreateVersion7(),
            PostId = postId,
            ImageId = imageId,
            Image = image,
            LocalPath = $"images/{imageId}",
        };

        dbContext.Images.Add(image);
        dbContext.PostMarkdownImages.Add(postMarkdownImage);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminPostOperationResult<PostMarkdownImageResponse>.Success(
            MarkdownDocumentMapper.ToResponse(postMarkdownImage)
        );
    }

    private static AdminPostOperationStatus ValidateImage(IFormFile file)
    {
        if (file.Length == 0)
        {
            return AdminPostOperationStatus.EmptyImageFile;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return AdminPostOperationStatus.ImageFileTooLarge;
        }

        var contentType = file.ContentType.Trim().ToLowerInvariant();
        return AllowedContentTypes.Contains(contentType)
            ? AdminPostOperationStatus.Success
            : AdminPostOperationStatus.UnsupportedImageContentType;
    }
}
