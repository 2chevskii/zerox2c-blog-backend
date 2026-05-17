namespace ZeroX2C.Blog.API.Modules.Posts;

public enum PostCommentOperationStatus
{
    Success,
    PostNotFound,
    ParentCommentNotFound,
    CommentNotFound,
    NotCommentAuthor,
    EmptyBody,
}
