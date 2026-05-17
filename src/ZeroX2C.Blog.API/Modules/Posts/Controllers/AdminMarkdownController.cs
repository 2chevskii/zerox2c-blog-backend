using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Posts.Admin;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Users.Auth;

namespace ZeroX2C.Blog.API.Modules.Posts.Controllers;

[ApiController, Route("api/admin/markdown")]
[Authorize(Policy = AuthorizationPolicyNames.Admin)]
public sealed class AdminMarkdownController(IAdminMarkdownService adminMarkdownService)
    : ControllerBase
{
    [HttpPost("render")]
    public async Task<ActionResult<MarkdownDocumentResponse>> RenderMarkdown(
        RenderMarkdownRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await adminMarkdownService.RenderMarkdownAsync(request, cancellationToken);
        return result.Status switch
        {
            AdminPostOperationStatus.Success => result.Value!,
            AdminPostOperationStatus.PostNotFound => NotFound(),
            AdminPostOperationStatus.InvalidMarkdownImageReference => ValidationProblem(
                "Markdown contains one or more unknown local image references."
            ),
            _ => Problem(),
        };
    }
}
