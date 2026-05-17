using ZeroX2C.Blog.API.Modules.Posts.Contracts;

namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public interface IAdminMarkdownService
{
    Task<AdminPostOperationResult<MarkdownDocumentResponse>> RenderMarkdownAsync(
        RenderMarkdownRequest request,
        CancellationToken cancellationToken
    );
}
