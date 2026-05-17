using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Assets.Images;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Posts.Markdown;
using ZeroX2C.Blog.API.Modules.Posts.Tags;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed class AdminPostService(
    BlogDbContext dbContext,
    IAuthenticationContext authenticationContext,
    IMarkdownDocumentRenderer markdownDocumentRenderer
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
        var post = await FindActivePostForEditAsync(id, cancellationToken);

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

        var imagesResult = await ValidatePostImagesAsync(
            request.CoverImageId,
            request.BannerImageId,
            cancellationToken
        );
        if (imagesResult.Status != AdminPostOperationStatus.Success)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(imagesResult.Status);
        }

        var renderResult = markdownDocumentRenderer.Render(request.BodyMarkdown, []);
        if (!renderResult.IsSuccess)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(
                AdminPostOperationStatus.InvalidMarkdownImageReference
            );
        }

        var postId = Guid.CreateVersion7();
        var post = new Post
        {
            Id = postId,
            Slug = slugResult.Value,
            Title = request.Title.Trim(),
            Subtitle = NormalizeOptionalText(request.Subtitle),
            CoverImageId = request.CoverImageId,
            BannerImageId = request.BannerImageId,
            Status = PostStatus.Draft,
            MarkdownDraft = new PostMarkdownDraft
            {
                Id = Guid.CreateVersion7(),
                PostId = postId,
                Document = renderResult.Document!,
            },
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
        var post = await FindActivePostForEditAsync(id, cancellationToken);
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

        var imagesResult = await ValidatePostImagesAsync(
            request.CoverImageId,
            request.BannerImageId,
            cancellationToken
        );
        if (imagesResult.Status != AdminPostOperationStatus.Success)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(imagesResult.Status);
        }

        var renderResult = markdownDocumentRenderer.Render(
            request.BodyMarkdown,
            post.MarkdownImages.Where(image => !image.IsDeleted).ToArray()
        );
        if (!renderResult.IsSuccess)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(
                AdminPostOperationStatus.InvalidMarkdownImageReference
            );
        }

        post.Slug = slugResult.Value;
        post.Title = request.Title.Trim();
        post.Subtitle = NormalizeOptionalText(request.Subtitle);
        post.CoverImageId = request.CoverImageId;
        post.BannerImageId = request.BannerImageId;
        UpsertDraft(post, renderResult.Document!);

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
        var post = await FindActivePostForEditAsync(id, cancellationToken);
        if (post is null)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(
                AdminPostOperationStatus.PostNotFound
            );
        }

        if (post.MarkdownDraft is null)
        {
            return AdminPostOperationResult<AdminPostResponse>.Failure(
                AdminPostOperationStatus.InvalidMarkdownImageReference
            );
        }

        var now = DateTime.UtcNow;
        post.Status = PostStatus.Published;
        post.PublishedBy = authenticationContext.UserId;
        post.PublishedAt = now;
        UpsertPublishedDocument(post, post.MarkdownDraft.Document);

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
        var post = await FindActivePostForEditAsync(id, cancellationToken);
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
        var post = await FindActivePostForEditAsync(id, cancellationToken);
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

    private Task<Post?> FindActivePostForEditAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext
            .Posts.Include(post => post.PostTags)
            .ThenInclude(postTag => postTag.Tag)
            .Include(post => post.MarkdownDraft)
            .Include(post => post.MarkdownDocument)
            .Include(post => post.MarkdownImages.Where(image => !image.IsDeleted))
            .ThenInclude(image => image.Image)
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

    private async Task<PostImageOperationResult> ValidatePostImagesAsync(
        Guid? coverImageId,
        Guid? bannerImageId,
        CancellationToken cancellationToken
    )
    {
        var imageIds = new[] { coverImageId, bannerImageId }
            .Where(imageId => imageId.HasValue)
            .Select(imageId => imageId!.Value)
            .Distinct()
            .ToArray();
        if (imageIds.Length == 0)
        {
            return PostImageOperationResult.Success();
        }

        var images = await dbContext.Images
            .Where(image => imageIds.Contains(image.Id) && !image.IsDeleted)
            .ToDictionaryAsync(image => image.Id, cancellationToken);

        if (images.Count != imageIds.Length)
        {
            return PostImageOperationResult.Failure(AdminPostOperationStatus.ImageNotFound);
        }

        if (
            coverImageId is Guid coverId
            && images[coverId].Purpose != ImagePurpose.Cover
        )
        {
            return PostImageOperationResult.Failure(
                AdminPostOperationStatus.InvalidImagePurpose
            );
        }

        if (
            bannerImageId is Guid bannerId
            && images[bannerId].Purpose != ImagePurpose.Banner
        )
        {
            return PostImageOperationResult.Failure(
                AdminPostOperationStatus.InvalidImagePurpose
            );
        }

        return PostImageOperationResult.Success();
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

    private void UpsertDraft(Post post, MarkdownDocumentContent document)
    {
        if (post.MarkdownDraft is null)
        {
            post.MarkdownDraft = new PostMarkdownDraft
            {
                Id = Guid.CreateVersion7(),
                PostId = post.Id,
                Document = document,
            };
            dbContext.PostMarkdownDrafts.Add(post.MarkdownDraft);
            return;
        }

        post.MarkdownDraft.Document = document;
    }

    private void UpsertPublishedDocument(Post post, MarkdownDocumentContent document)
    {
        if (post.MarkdownDocument is null)
        {
            post.MarkdownDocument = new PostMarkdownDocument
            {
                Id = Guid.CreateVersion7(),
                PostId = post.Id,
                Document = PostMapper.CloneDocument(document),
            };
            dbContext.PostMarkdownDocuments.Add(post.MarkdownDocument);
            return;
        }

        post.MarkdownDocument.Document = PostMapper.CloneDocument(document);
    }

    private IQueryable<Post> ActivePostsQuery() =>
        dbContext
            .Posts.Include(post => post.PostTags.Where(postTag => !postTag.IsDeleted))
            .ThenInclude(postTag => postTag.Tag)
            .Include(post => post.MarkdownDraft)
            .Where(post => !post.IsDeleted);

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
