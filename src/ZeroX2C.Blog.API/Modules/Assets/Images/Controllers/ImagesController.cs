using Microsoft.AspNetCore.Mvc;

namespace ZeroX2C.Blog.API.Modules.Assets.Images.Controllers;

[ApiController, Route("api/images")]
public sealed class ImagesController(IImageQueryService imageQueryService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetImage(Guid id, CancellationToken cancellationToken)
    {
        var image = await imageQueryService.GetImageAsync(id, cancellationToken);
        return image is null
            ? NotFound()
            : File(image.Content, image.ContentType);
    }
}
