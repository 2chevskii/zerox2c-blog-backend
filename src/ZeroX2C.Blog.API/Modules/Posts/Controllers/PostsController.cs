using Microsoft.AspNetCore.Mvc;

namespace ZeroX2C.Blog.API.Modules.Posts.Controllers;

[ApiController, Route("api/posts")]
public sealed class PostsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetPostList(
        int offset = 0,
        int limit = 10,
        string? search = null
    ) =>
        Ok(Array.Empty<object>());

    [HttpGet("{id:guid}")]
    public IActionResult GetPostDetails(Guid id) => NotFound();

    // ReSharper disable once RouteTemplates.RouteParameterConstraintNotResolved
    [HttpGet("{slug:slug}")]
    public IActionResult GetPostDetails(string slug) => NotFound();
}
