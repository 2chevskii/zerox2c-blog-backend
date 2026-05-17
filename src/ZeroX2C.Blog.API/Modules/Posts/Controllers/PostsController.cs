using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Posts;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Users.Auth;

namespace ZeroX2C.Blog.API.Modules.Posts.Controllers;

[ApiController, Route("api/posts")]
public sealed class PostsController(
    IPostQueryService postQueryService,
    IPostReactionService postReactionService
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PostListItemResponse>>> GetPostList(
        [Range(0, int.MaxValue)]
        int offset = 0,
        [Range(1, 100)]
        int limit = 10,
        string? search = null,
        string? tags = null,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await postQueryService.GetPublishedPostsAsync(
                offset,
                limit,
                search,
                tags,
                from,
                to,
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

    [HttpGet("{id:guid}/reaction")]
    [Authorize(Policy = AuthorizationPolicyNames.AuthenticatedNotBlocked)]
    public async Task<ActionResult<PostReactionResponse>> GetReaction(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var reaction = await postReactionService.GetReactionAsync(id, cancellationToken);
        return reaction is null ? NotFound() : reaction;
    }

    [HttpPut("{id:guid}/reaction")]
    [Authorize(Policy = AuthorizationPolicyNames.AuthenticatedNotBlocked)]
    public async Task<ActionResult<PostReactionResponse>> SetReaction(
        Guid id,
        PostReactionRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await postReactionService.SetReactionAsync(
            id,
            request.Reaction,
            cancellationToken
        );

        return ToReactionActionResult(result);
    }

    [HttpDelete("{id:guid}/reaction")]
    [Authorize(Policy = AuthorizationPolicyNames.AuthenticatedNotBlocked)]
    public async Task<ActionResult<PostReactionResponse>> ClearReaction(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var result = await postReactionService.ClearReactionAsync(id, cancellationToken);
        return ToReactionActionResult(result);
    }

    private ActionResult<PostReactionResponse> ToReactionActionResult(
        PostReactionOperationResult result
    ) =>
        result.Status switch
        {
            PostReactionOperationStatus.Success => result.Response!,
            PostReactionOperationStatus.PostNotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
}
