using ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;

namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public interface IAdminTagService
{
    Task<IReadOnlyCollection<AdminTagResponse>> GetTagsAsync(
        int offset,
        int limit,
        string? search,
        CancellationToken cancellationToken
    );

    Task<AdminTagOperationResult<AdminTagResponse>> GetTagAsync(
        Guid id,
        CancellationToken cancellationToken
    );

    Task<AdminTagOperationResult<AdminTagResponse>> CreateTagAsync(
        CreateTagRequest request,
        CancellationToken cancellationToken
    );

    Task<AdminTagOperationResult<AdminTagResponse>> UpdateTagAsync(
        Guid id,
        UpdateTagRequest request,
        CancellationToken cancellationToken
    );

    Task<AdminTagOperationResult> DeleteTagAsync(
        Guid id,
        CancellationToken cancellationToken
    );
}
