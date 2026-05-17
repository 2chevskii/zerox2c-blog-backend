namespace ZeroX2C.Blog.API.Modules.Users.Profile;

public sealed record ProfileOperationResult<T>(ProfileOperationStatus Status, T? Value = default)
{
    public static ProfileOperationResult<T> Success(T value) =>
        new(ProfileOperationStatus.Success, value);

    public static ProfileOperationResult<T> Failure(ProfileOperationStatus status) => new(status);
}
