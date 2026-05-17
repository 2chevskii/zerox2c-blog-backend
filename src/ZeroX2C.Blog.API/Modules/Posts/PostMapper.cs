using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;
using ZeroX2C.Blog.API.Modules.Posts.Markdown;
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
            post.LikeCount,
            post.DislikeCount,
            post.CommentCount,
            post.ViewCount,
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
            GetPublishedDocument(post).Html,
            GetPublishedDocument(post).ReadingMinutes,
            post.LikeCount,
            post.DislikeCount,
            post.CommentCount,
            post.ViewCount,
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
            GetDraftDocument(post).Markdown,
            GetDraftDocument(post).Html,
            GetDraftDocument(post).ReadingMinutes,
            post.Status,
            post.LikeCount,
            post.DislikeCount,
            post.CommentCount,
            post.ViewCount,
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

    public static MarkdownDocumentContent CloneDocument(MarkdownDocumentContent document) =>
        new()
        {
            Markdown = document.Markdown,
            Html = document.Html,
            PlainText = document.PlainText,
            ReadingMinutes = document.ReadingMinutes,
        };

    private static MarkdownDocumentContent GetDraftDocument(Post post) =>
        post.MarkdownDraft?.Document ?? EmptyDocument();

    private static MarkdownDocumentContent GetPublishedDocument(Post post) =>
        post.MarkdownDocument?.Document ?? EmptyDocument();

    private static MarkdownDocumentContent EmptyDocument() =>
        new()
        {
            Markdown = string.Empty,
            Html = string.Empty,
            PlainText = string.Empty,
            ReadingMinutes = 1,
        };

    private static IReadOnlyCollection<TagResponse> ToTagResponses(Post post) =>
        post.PostTags
            .Where(postTag => !postTag.IsDeleted && !postTag.Tag.IsDeleted)
            .OrderBy(postTag => postTag.Tag.Name)
            .Select(postTag => TagMapper.ToResponse(postTag.Tag))
            .ToArray();
}
