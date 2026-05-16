using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Assets.Images.Contracts;
using ZeroX2C.Blog.API.Modules.Users.Auth;

namespace ZeroX2C.Blog.API.Modules.Assets.Images.Controllers;

[ApiController, Route("api/admin/images")]
[Authorize(Policy = AuthorizationPolicyNames.Admin)]
public sealed class AdminImagesController(IAdminImageService adminImageService)
    : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ImageResponse>> UploadImage(
        [FromForm] UploadImageRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await adminImageService.UploadImageAsync(request, cancellationToken);
        return result.Status switch
        {
            AdminImageOperationStatus.Success => Created(result.Value!.Url, result.Value),
            AdminImageOperationStatus.EmptyFile => ValidationProblem("Image file is empty."),
            AdminImageOperationStatus.FileTooLarge => ValidationProblem(
                "Image file must not exceed 5 MB."
            ),
            AdminImageOperationStatus.UnsupportedContentType => ValidationProblem(
                "Image file must be JPEG, PNG, WebP, or GIF."
            ),
            _ => Problem(),
        };
    }
}
