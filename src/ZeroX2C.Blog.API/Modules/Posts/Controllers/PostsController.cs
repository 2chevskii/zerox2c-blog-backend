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
    IPostReactionService postReactionService,
    IPostCommentService postCommentService
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

    [HttpGet("keywords")]
    public async Task<ActionResult<IReadOnlyCollection<string>>> GetSearchKeywords(
        string? search = null,
        [Range(1, 20)]
        int limit = 6,
        CancellationToken cancellationToken = default
    ) =>
        Ok(await postQueryService.GetPublishedSearchKeywordsAsync(search, limit, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostDetailsResponse>> GetPostDetails(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var post = await postQueryService.GetPublishedPostByIdAsync(id, cancellationToken);
        return post is null ? NotFound() : post;
    }

    [HttpGet("slugs/{slug:slug}/id")]
    public async Task<ActionResult<PostSlugResolutionResponse>> ResolveSlug(
        string slug,
        CancellationToken cancellationToken
    )
    {
        var postId = await postQueryService.GetPublishedPostIdBySlugAsync(slug, cancellationToken);
        return postId is null ? NotFound() : new PostSlugResolutionResponse(postId.Value);
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<IReadOnlyCollection<PostCommentResponse>>> GetComments(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var comments = await postCommentService.GetVisibleCommentsAsync(id, cancellationToken);
        return comments is null ? NotFound() : Ok(comments);
    }

    [HttpPost("{id:guid}/comments")]
    [Authorize(Policy = AuthorizationPolicyNames.AuthenticatedNotBlocked)]
    public async Task<ActionResult<PostCommentResponse>> CreateComment(
        Guid id,
        PostCommentRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await postCommentService.CreateCommentAsync(
            id,
            request.Body,
            request.ParentCommentId,
            cancellationToken
        );

        return ToCommentActionResult(result);
    }

    [HttpPut("{postId:guid}/comments/{commentId:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.AuthenticatedNotBlocked)]
    public async Task<ActionResult<PostCommentResponse>> UpdateComment(
        Guid postId,
        Guid commentId,
        UpdatePostCommentRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await postCommentService.UpdateCommentAsync(
            postId,
            commentId,
            request.Body,
            cancellationToken
        );

        return ToCommentActionResult(result);
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

    private ActionResult<PostCommentResponse> ToCommentActionResult(
        PostCommentOperationResult result
    ) =>
        result.Status switch
        {
            PostCommentOperationStatus.Success => result.Response!,
            PostCommentOperationStatus.PostNotFound => NotFound(),
            PostCommentOperationStatus.ParentCommentNotFound => ValidationProblem(
                "Parent comment was not found."
            ),
            PostCommentOperationStatus.CommentNotFound => NotFound(),
            PostCommentOperationStatus.NotCommentAuthor => Forbid(),
            PostCommentOperationStatus.EmptyBody => ValidationProblem("Comment body is required."),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
}
