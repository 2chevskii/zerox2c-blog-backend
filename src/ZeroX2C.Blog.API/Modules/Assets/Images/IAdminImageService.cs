using ZeroX2C.Blog.API.Modules.Assets.Images.Contracts;

namespace ZeroX2C.Blog.API.Modules.Assets.Images;

public interface IAdminImageService
{
    Task<AdminImageOperationResult<ImageResponse>> UploadImageAsync(
        UploadImageRequest request,
        CancellationToken cancellationToken
    );
}
