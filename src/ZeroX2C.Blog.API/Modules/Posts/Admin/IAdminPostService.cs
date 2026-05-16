using ZeroX2C.Blog.API.Modules.Posts.Contracts;

namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public interface IAdminPostService
{
    Task<IReadOnlyCollection<AdminPostResponse>> GetPostsAsync(
        int offset,
        int limit,
        PostStatus? status,
        string? search,
        CancellationToken cancellationToken
    );

    Task<AdminPostOperationResult<AdminPostResponse>> GetPostAsync(
        Guid id,
        CancellationToken cancellationToken
    );

    Task<AdminPostOperationResult<AdminPostResponse>> CreatePostAsync(
        CreatePostRequest request,
        CancellationToken cancellationToken
    );

    Task<AdminPostOperationResult<AdminPostResponse>> UpdatePostAsync(
        Guid id,
        UpdatePostRequest request,
        CancellationToken cancellationToken
    );

    Task<AdminPostOperationResult<AdminPostResponse>> PublishPostAsync(
        Guid id,
        CancellationToken cancellationToken
    );

    Task<AdminPostOperationResult<AdminPostResponse>> UnpublishPostAsync(
        Guid id,
        CancellationToken cancellationToken
    );

    Task<AdminPostOperationResult> DeletePostAsync(
        Guid id,
        CancellationToken cancellationToken
    );
}
