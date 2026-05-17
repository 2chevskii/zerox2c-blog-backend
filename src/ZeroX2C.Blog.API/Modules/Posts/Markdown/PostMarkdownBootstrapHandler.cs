using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.CrossCutting.Bootstrap;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Posts.Markdown;

public sealed class PostMarkdownBootstrapHandler(
    BlogDbContext dbContext,
    IMarkdownDocumentRenderer markdownDocumentRenderer,
    IAuthenticationContextManager authenticationContextManager,
    ILogger<PostMarkdownBootstrapHandler> logger
) : IBootstrapHandler
{
    public int Order => 10;

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        using var authenticationScope = authenticationContextManager.AsSystem();

        var posts = await dbContext.Posts
            .Include(post => post.MarkdownDraft)
            .Include(post => post.MarkdownDocument)
            .Where(post => !post.IsDeleted)
            .ToArrayAsync(cancellationToken);
        var changed = false;

        foreach (var post in posts)
        {
            if (post.MarkdownDraft is null)
            {
                var legacyBody =
                    dbContext.Entry(post).Property<string>("Body").CurrentValue ?? string.Empty;
                var renderResult = markdownDocumentRenderer.Render(legacyBody, []);
                if (!renderResult.IsSuccess)
                {
                    logger.LogWarning(
                        "Skipped Markdown backfill for post {PostId} because legacy content contains invalid local image references.",
                        post.Id
                    );
                    continue;
                }

                post.MarkdownDraft = new PostMarkdownDraft
                {
                    Id = Guid.CreateVersion7(),
                    PostId = post.Id,
                    Document = renderResult.Document!,
                };
                dbContext.PostMarkdownDrafts.Add(post.MarkdownDraft);
                changed = true;
            }

            if (post.Status == PostStatus.Published && post.MarkdownDocument is null)
            {
                post.MarkdownDocument = new PostMarkdownDocument
                {
                    Id = Guid.CreateVersion7(),
                    PostId = post.Id,
                    Document = PostMapper.CloneDocument(post.MarkdownDraft.Document),
                };
                dbContext.PostMarkdownDocuments.Add(post.MarkdownDocument);
                changed = true;
            }
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
