using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Posts.Admin;
using ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;
using ZeroX2C.Blog.API.Modules.Users.Auth;

namespace ZeroX2C.Blog.API.Modules.Posts.Controllers;

[ApiController, Route("api/admin/tags")]
[Authorize(Policy = AuthorizationPolicyNames.Admin)]
public sealed class AdminTagsController(IAdminTagService adminTagService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminTagResponse>>> GetTags(
        [Range(0, int.MaxValue)] int offset = 0,
        [Range(1, 100)] int limit = 50,
        string? search = null,
        CancellationToken cancellationToken = default
    ) =>
        Ok(await adminTagService.GetTagsAsync(offset, limit, search, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminTagResponse>> GetTag(
        Guid id,
        CancellationToken cancellationToken
    ) =>
        ToActionResult(await adminTagService.GetTagAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AdminTagResponse>> CreateTag(
        CreateTagRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await adminTagService.CreateTagAsync(request, cancellationToken);
        return result.Status == AdminTagOperationStatus.Success
            ? CreatedAtAction(nameof(GetTag), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminTagResponse>> UpdateTag(
        Guid id,
        UpdateTagRequest request,
        CancellationToken cancellationToken
    ) =>
        ToActionResult(await adminTagService.UpdateTagAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminTagService.DeleteTagAsync(id, cancellationToken);
        return result.Status switch
        {
            AdminTagOperationStatus.Success => NoContent(),
            AdminTagOperationStatus.TagNotFound => NotFound(),
            _ => Problem(),
        };
    }

    private ActionResult<AdminTagResponse> ToActionResult(
        AdminTagOperationResult<AdminTagResponse> result
    ) =>
        result.Status switch
        {
            AdminTagOperationStatus.Success => result.Value!,
            AdminTagOperationStatus.TagNotFound => NotFound(),
            AdminTagOperationStatus.NameAlreadyTaken => ValidationProblem(
                "Tag name is already taken."
            ),
            AdminTagOperationStatus.InvalidName => ValidationProblem("Tag name is invalid."),
            _ => Problem(),
        };

}
