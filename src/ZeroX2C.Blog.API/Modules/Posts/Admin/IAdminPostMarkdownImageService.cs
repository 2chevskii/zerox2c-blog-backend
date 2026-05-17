using ZeroX2C.Blog.API.Modules.Posts.Contracts;

namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public interface IAdminPostMarkdownImageService
{
    Task<AdminPostOperationResult<IReadOnlyCollection<PostMarkdownImageResponse>>> GetImagesAsync(
        Guid postId,
        CancellationToken cancellationToken
    );

    Task<AdminPostOperationResult<PostMarkdownImageResponse>> UploadImageAsync(
        Guid postId,
        UploadPostMarkdownImageRequest request,
        CancellationToken cancellationToken
    );
}
