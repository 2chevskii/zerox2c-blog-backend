using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;
using ZeroX2C.Blog.API.Modules.Posts.Tags;

namespace ZeroX2C.Blog.API.Modules.Posts;

public static class PostMapper
{
    public static PostListItemResponse ToListItemResponse(Post post) =>
        new(
            post.Id,
            post.Slug,
            post.Title,
            post.Subtitle,
            post.Excerpt,
            post.CoverImageId,
            post.BannerImageId,
            ToTagResponses(post),
            post.PublishedAt
        );

    public static PostDetailsResponse ToDetailsResponse(Post post) =>
        new(
            post.Id,
            post.Slug,
            post.Title,
            post.Subtitle,
            post.Excerpt,
            post.Body,
            post.CoverImageId,
            post.BannerImageId,
            ToTagResponses(post),
            post.PublishedAt
        );

    public static AdminPostResponse ToAdminResponse(Post post) =>
        new(
            post.Id,
            post.Slug,
            post.Title,
            post.Subtitle,
            post.Excerpt,
            post.Body,
            post.Status,
            post.CoverImageId,
            post.BannerImageId,
            ToTagResponses(post),
            post.CreatedBy,
            post.CreatedAt,
            post.UpdatedBy,
            post.UpdatedAt,
            post.PublishedBy,
            post.PublishedAt,
            post.DeletedBy,
            post.DeletedAt
        );

    private static IReadOnlyCollection<TagResponse> ToTagResponses(Post post) =>
        post.PostTags
            .Where(postTag => !postTag.IsDeleted && !postTag.Tag.IsDeleted)
            .OrderBy(postTag => postTag.Tag.Name)
            .Select(postTag => TagMapper.ToResponse(postTag.Tag))
            .ToArray();
}
