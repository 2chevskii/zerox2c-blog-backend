using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;

namespace ZeroX2C.Blog.API.Modules.Posts.Controllers;

[ApiController, Route("api/tags")]
public sealed class TagsController(ITagQueryService tagQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<TagResponse>>> GetTags(
        [Range(0, int.MaxValue)] int offset = 0,
        [Range(1, 100)] int limit = 100,
        string? search = null,
        CancellationToken cancellationToken = default
    ) =>
        Ok(await tagQueryService.GetTagsAsync(offset, limit, search, cancellationToken));
}
