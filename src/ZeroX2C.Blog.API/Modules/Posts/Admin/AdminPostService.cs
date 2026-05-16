using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Posts.Tags;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed class AdminPostService(
    BlogDbContext dbContext,
    IAuthenticationContext authenticationContext
) : IAdminPostService
{
    public async Task<IReadOnlyCollection<AdminPostResponse>> GetPostsAsync(
        int offset,
        int limit,
        PostStatus? status,
        string? search,
        CancellationToken cancellationToken
    )
    {
        var query = ActivePostsQuery();

        if (status is not null)
        {
            query = query.Where(post => post.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(post =>
                post.Title.Contains(normalizedSearch)
                || (post.Subtitle != null && post.Subtitle.Contains(normalizedSearch))
                || (post.Excerpt != null && post.Excerpt.Contains(normalizedSearch))
                || (post.Slug != null && post.Slug.Contains(normalizedSearch))
            );
        }

        var posts = await query
            .OrderByDescending(post => post.CreatedAt)
            .ThenByDescending(post => post.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return posts.Select(PostMapper.ToAdminResponse).ToArray();
    }

    public async Task<AdminPostOperationResult<AdminPostResponse>> GetPostAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var post = await FindActivePostWithTagsAsync(id, cancellationToken);

        return post is null
            ? AdminPostOperationResult<AdminPostResponse>.Failure(
                AdminPostOperationStatus.PostNotFound
            )
            : AdminPostOperationResult<AdminPostResponse>.Success(PostMapper.ToAdminResponse(post));
    }

    public async Task<AdminPostOperationResult<AdminPostResponse>> CreatePostAsync(
        CreatePostRequest request,
        CancellationToken cancellationToken
    )
    {
        var slugResult = await NormalizeAndValidateSlugAsync(
            request.Slug,
            request.Title,
            excludedPostId: null,
            cancellationToken
        );
        if (slugResult.Status != AdminPostOperationStatus.Success)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(slugResult.Status);
        }

        var tagsResult = await GetActiveTagsAsync(request.TagIds, cancellationToken);
        if (tagsResult.Status != AdminPostOperationStatus.Success)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(tagsResult.Status);
        }

        var postId = Guid.CreateVersion7();
        var post = new Post
        {
            Id = postId,
            Slug = slugResult.Value,
            Title = request.Title.Trim(),
            Subtitle = NormalizeOptionalText(request.Subtitle),
            Excerpt = NormalizeOptionalText(request.Excerpt),
            Body = request.Body,
            CoverImageId = request.CoverImageId,
            BannerImageId = request.BannerImageId,
            Status = PostStatus.Draft,
            PostTags = tagsResult
                .Value!.Select(tag => CreatePostTag(postId, tag))
                .ToList(),
        };

        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminPostOperationResult<AdminPostResponse>.Success(
            PostMapper.ToAdminResponse(post)
        );
    }

    public async Task<AdminPostOperationResult<AdminPostResponse>> UpdatePostAsync(
        Guid id,
        UpdatePostRequest request,
        CancellationToken cancellationToken
    )
    {
        var post = await FindActivePostWithTagsAsync(id, cancellationToken);
        if (post is null)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(
                AdminPostOperationStatus.PostNotFound
            );
        }

        var slugResult = await NormalizeAndValidateSlugAsync(
            request.Slug,
            request.Title,
            excludedPostId: id,
            cancellationToken
        );
        if (slugResult.Status != AdminPostOperationStatus.Success)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(slugResult.Status);
        }

        var tagsResult = await GetActiveTagsAsync(request.TagIds, cancellationToken);
        if (tagsResult.Status != AdminPostOperationStatus.Success)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(tagsResult.Status);
        }

        post.Slug = slugResult.Value;
        post.Title = request.Title.Trim();
        post.Subtitle = NormalizeOptionalText(request.Subtitle);
        post.Excerpt = NormalizeOptionalText(request.Excerpt);
        post.Body = request.Body;
        post.CoverImageId = request.CoverImageId;
        post.BannerImageId = request.BannerImageId;

        ReplaceTags(post, tagsResult.Value!);

        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminPostOperationResult<AdminPostResponse>.Success(
            PostMapper.ToAdminResponse(post)
        );
    }

    public async Task<AdminPostOperationResult<AdminPostResponse>> PublishPostAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var post = await FindActivePostWithTagsAsync(id, cancellationToken);
        if (post is null)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(
                AdminPostOperationStatus.PostNotFound
            );
        }

        var now = DateTime.UtcNow;
        post.Status = PostStatus.Published;
        post.PublishedBy = authenticationContext.UserId;
        post.PublishedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminPostOperationResult<AdminPostResponse>.Success(
            PostMapper.ToAdminResponse(post)
        );
    }

    public async Task<AdminPostOperationResult<AdminPostResponse>> UnpublishPostAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var post = await FindActivePostWithTagsAsync(id, cancellationToken);
        if (post is null)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(
                AdminPostOperationStatus.PostNotFound
            );
        }

        post.Status = PostStatus.Draft;
        post.PublishedBy = null;
        post.PublishedAt = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminPostOperationResult<AdminPostResponse>.Success(
            PostMapper.ToAdminResponse(post)
        );
    }

    public async Task<AdminPostOperationResult> DeletePostAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var post = await FindActivePostWithTagsAsync(id, cancellationToken);
        if (post is null)
        {
            return AdminPostOperationResult.Failure(AdminPostOperationStatus.PostNotFound);
        }

        dbContext.Posts.Remove(post);

        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminPostOperationResult.Success();
    }

    private async Task<SlugOperationResult> NormalizeAndValidateSlugAsync(
        string? slug,
        string title,
        Guid? excludedPostId,
        CancellationToken cancellationToken
    )
    {
        var normalizedSlug = PostSlug.Normalize(slug);
        if (normalizedSlug is null)
        {
            return SlugOperationResult.Success(
                await GenerateUniqueSlugAsync(title, excludedPostId, cancellationToken)
            );
        }

        if (!PostSlug.IsValid(normalizedSlug))
        {
            return SlugOperationResult.Failure(AdminPostOperationStatus.InvalidSlug);
        }

        var isSlugTaken = await dbContext.Posts.AnyAsync(
            post =>
                post.Slug == normalizedSlug
                && (excludedPostId == null || post.Id != excludedPostId),
            cancellationToken
        );

        return isSlugTaken
            ? SlugOperationResult.Failure(AdminPostOperationStatus.SlugAlreadyTaken)
            : SlugOperationResult.Success(normalizedSlug);
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string title,
        Guid? excludedPostId,
        CancellationToken cancellationToken
    )
    {
        var baseSlug = PostSlug.CreateFromTitle(title);
        var candidate = baseSlug;
        var suffix = 2;

        while (
            await dbContext.Posts.AnyAsync(
                post =>
                    post.Slug == candidate
                    && (excludedPostId == null || post.Id != excludedPostId),
                cancellationToken
            )
        )
        {
            candidate = PostSlug.AddNumericSuffix(baseSlug, suffix);
            suffix++;
        }

        return candidate;
    }

    private Task<Post?> FindActivePostWithTagsAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext
            .Posts.Include(post => post.PostTags)
            .ThenInclude(postTag => postTag.Tag)
            .SingleOrDefaultAsync(
                existingPost => existingPost.Id == id && !existingPost.IsDeleted,
                cancellationToken
            );

    private async Task<TagCollectionOperationResult> GetActiveTagsAsync(
        IReadOnlyCollection<Guid>? tagIds,
        CancellationToken cancellationToken
    )
    {
        var requestedTagIds = tagIds?.Distinct().ToArray() ?? [];
        if (requestedTagIds.Length == 0)
        {
            return TagCollectionOperationResult.Success([]);
        }

        var tags = await dbContext.Tags
            .Where(tag => requestedTagIds.Contains(tag.Id) && !tag.IsDeleted)
            .ToListAsync(cancellationToken);

        return tags.Count == requestedTagIds.Length
            ? TagCollectionOperationResult.Success(tags)
            : TagCollectionOperationResult.Failure(AdminPostOperationStatus.TagNotFound);
    }

    private static PostTag CreatePostTag(Guid postId, Tag tag) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            PostId = postId,
            TagId = tag.Id,
            Tag = tag,
        };

    private void ReplaceTags(Post post, IReadOnlyCollection<Tag> tags)
    {
        var tagIds = tags.Select(tag => tag.Id).ToHashSet();

        foreach (var postTag in post.PostTags.Where(postTag => !postTag.IsDeleted))
        {
            if (tagIds.Contains(postTag.TagId))
            {
                continue;
            }

            dbContext.PostTags.Remove(postTag);
        }

        var existingTagIds = post.PostTags
            .Where(postTag => !postTag.IsDeleted)
            .Select(postTag => postTag.TagId)
            .ToHashSet();

        foreach (var tag in tags.Where(tag => !existingTagIds.Contains(tag.Id)))
        {
            var deletedPostTag = post.PostTags.SingleOrDefault(postTag =>
                postTag.TagId == tag.Id && postTag.IsDeleted
            );

            if (deletedPostTag is null)
            {
                var postTag = CreatePostTag(post.Id, tag);
                post.PostTags.Add(postTag);
                dbContext.PostTags.Add(postTag);
                continue;
            }

            deletedPostTag.IsDeleted = false;
            deletedPostTag.Tag = tag;
        }
    }

    private IQueryable<Post> ActivePostsQuery() =>
        dbContext
            .Posts.Include(post => post.PostTags.Where(postTag => !postTag.IsDeleted))
            .ThenInclude(postTag => postTag.Tag)
            .Where(post => !post.IsDeleted);

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
