namespace ZeroX2C.Blog.API.Modules.Users.Profile;

public enum ProfileOperationStatus
{
    Success,
    UserNotFound,
    EmptyFile,
    FileTooLarge,
    UnsupportedContentType,
    ReplyNotFound,
}
