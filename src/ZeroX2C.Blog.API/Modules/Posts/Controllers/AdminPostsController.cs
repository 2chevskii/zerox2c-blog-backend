using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Posts.Admin;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Users.Auth;

namespace ZeroX2C.Blog.API.Modules.Posts.Controllers;

[ApiController, Route("api/admin/posts")]
[Authorize(Policy = AuthorizationPolicyNames.Admin)]
public sealed class AdminPostsController(IAdminPostService adminPostService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminPostResponse>>> GetPosts(
        [Range(0, int.MaxValue)]
        int offset = 0,
        [Range(1, 100)]
        int limit = 50,
        PostStatus? status = null,
        string? search = null,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await adminPostService.GetPostsAsync(
                offset,
                limit,
                status,
                search,
                cancellationToken
            )
        );

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminPostResponse>> GetPost(
        Guid id,
        CancellationToken cancellationToken
    ) =>
        ToActionResult(await adminPostService.GetPostAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AdminPostResponse>> CreatePost(
        CreatePostRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await adminPostService.CreatePostAsync(request, cancellationToken);
        return result.Status == AdminPostOperationStatus.Success
            ? CreatedAtAction(nameof(GetPost), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminPostResponse>> UpdatePost(
        Guid id,
        UpdatePostRequest request,
        CancellationToken cancellationToken
    ) =>
        ToActionResult(await adminPostService.UpdatePostAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<AdminPostResponse>> PublishPost(
        Guid id,
        CancellationToken cancellationToken
    ) =>
        ToActionResult(await adminPostService.PublishPostAsync(id, cancellationToken));

    [HttpPost("{id:guid}/unpublish")]
    public async Task<ActionResult<AdminPostResponse>> UnpublishPost(
        Guid id,
        CancellationToken cancellationToken
    ) =>
        ToActionResult(await adminPostService.UnpublishPostAsync(id, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminPostService.DeletePostAsync(id, cancellationToken);
        return result.Status switch
        {
            AdminPostOperationStatus.Success => NoContent(),
            AdminPostOperationStatus.PostNotFound => NotFound(),
            _ => Problem(),
        };
    }

    private ActionResult<AdminPostResponse> ToActionResult(
        AdminPostOperationResult<AdminPostResponse> result
    ) =>
        result.Status switch
        {
            AdminPostOperationStatus.Success => result.Value!,
            AdminPostOperationStatus.PostNotFound => NotFound(),
            AdminPostOperationStatus.SlugAlreadyTaken => ValidationProblem(
                "Slug is already taken."
            ),
            AdminPostOperationStatus.InvalidSlug => ValidationProblem("Slug is invalid."),
            AdminPostOperationStatus.TagNotFound => ValidationProblem(
                "One or more tags were not found."
            ),
            AdminPostOperationStatus.ImageNotFound => ValidationProblem(
                "One or more images were not found."
            ),
            AdminPostOperationStatus.InvalidImagePurpose => ValidationProblem(
                "Cover image must use cover purpose and banner image must use banner purpose."
            ),
            _ => Problem(),
        };

}
