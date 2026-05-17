using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Posts.Admin;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Users.Auth;

namespace ZeroX2C.Blog.API.Modules.Posts.Controllers;

[ApiController, Route("api/admin/posts/{postId:guid}/markdown/images")]
[Authorize(Policy = AuthorizationPolicyNames.Admin)]
public sealed class AdminPostMarkdownImagesController(
    IAdminPostMarkdownImageService adminPostMarkdownImageService
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PostMarkdownImageResponse>>> GetImages(
        Guid postId,
        CancellationToken cancellationToken
    )
    {
        var result = await adminPostMarkdownImageService.GetImagesAsync(postId, cancellationToken);
        return result.Status switch
        {
            AdminPostOperationStatus.Success => result.Value!.ToArray(),
            AdminPostOperationStatus.PostNotFound => NotFound(),
            _ => Problem(),
        };
    }

    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<PostMarkdownImageResponse>> UploadImage(
        Guid postId,
        [FromForm] UploadPostMarkdownImageRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await adminPostMarkdownImageService.UploadImageAsync(
            postId,
            request,
            cancellationToken
        );

        return result.Status switch
        {
            AdminPostOperationStatus.Success => Created(result.Value!.Url, result.Value),
            AdminPostOperationStatus.PostNotFound => NotFound(),
            AdminPostOperationStatus.EmptyImageFile => ValidationProblem("Image file is empty."),
            AdminPostOperationStatus.ImageFileTooLarge => ValidationProblem(
                "Image file must not exceed 5 MB."
            ),
            AdminPostOperationStatus.UnsupportedImageContentType => ValidationProblem(
                "Image file must be JPEG, PNG, WebP, or GIF."
            ),
            _ => Problem(),
        };
    }
}
