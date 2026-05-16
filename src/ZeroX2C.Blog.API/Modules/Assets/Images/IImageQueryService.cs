namespace ZeroX2C.Blog.API.Modules.Assets.Images;

public interface IImageQueryService
{
    Task<Image?> GetImageAsync(Guid id, CancellationToken cancellationToken);
}
