using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;

namespace ZeroX2C.Blog.API.Modules.Posts.Controllers;

[ApiController, Route("api/posts")]
public sealed class PostsController(IPostQueryService postQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PostListItemResponse>>> GetPostList(
        [Range(0, int.MaxValue)]
        int offset = 0,
        [Range(1, 100)]
        int limit = 10,
        string? search = null,
        string? tags = null,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await postQueryService.GetPublishedPostsAsync(
                offset,
                limit,
                search,
                tags,
                cancellationToken
            )
        );

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostDetailsResponse>> GetPostDetails(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var post = await postQueryService.GetPublishedPostByIdAsync(id, cancellationToken);
        return post is null ? NotFound() : post;
    }

    // ReSharper disable once RouteTemplates.RouteParameterConstraintNotResolved
    [HttpGet("{slug:slug}")]
    public async Task<ActionResult<PostDetailsResponse>> GetPostDetails(
        string slug,
        CancellationToken cancellationToken
    )
    {
        var post = await postQueryService.GetPublishedPostBySlugAsync(slug, cancellationToken);
        return post is null ? NotFound() : post;
    }
}
