using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Assets.Images;

public sealed class ImageQueryService(BlogDbContext dbContext) : IImageQueryService
{
    public Task<Image?> GetImageAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Images.SingleOrDefaultAsync(
            image => image.Id == id && !image.IsDeleted,
            cancellationToken
        );
}
