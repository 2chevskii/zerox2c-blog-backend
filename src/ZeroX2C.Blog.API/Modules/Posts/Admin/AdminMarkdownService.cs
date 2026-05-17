using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Posts.Markdown;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed class AdminMarkdownService(
    BlogDbContext dbContext,
    IMarkdownDocumentRenderer markdownDocumentRenderer
) : IAdminMarkdownService
{
    public async Task<AdminPostOperationResult<MarkdownDocumentResponse>> RenderMarkdownAsync(
        RenderMarkdownRequest request,
        CancellationToken cancellationToken
    )
    {
        var images = Array.Empty<PostMarkdownImage>();
        if (request.PostId is Guid postId)
        {
            var postExists = await dbContext.Posts.AnyAsync(
                post => post.Id == postId && !post.IsDeleted,
                cancellationToken
            );
            if (!postExists)
            {
                return AdminPostOperationResult<MarkdownDocumentResponse>.Failure(
                    AdminPostOperationStatus.PostNotFound
                );
            }

            images = await dbContext.PostMarkdownImages
                .Include(image => image.Image)
                .Where(image =>
                    image.PostId == postId
                    && !image.IsDeleted
                    && !image.Image.IsDeleted
                )
                .ToArrayAsync(cancellationToken);
        }

        var renderResult = markdownDocumentRenderer.Render(request.Markdown, images);
        return renderResult.IsSuccess
            ? AdminPostOperationResult<MarkdownDocumentResponse>.Success(
                MarkdownDocumentMapper.ToResponse(renderResult.Document!)
            )
            : AdminPostOperationResult<MarkdownDocumentResponse>.Failure(
                AdminPostOperationStatus.InvalidMarkdownImageReference
            );
    }
}
