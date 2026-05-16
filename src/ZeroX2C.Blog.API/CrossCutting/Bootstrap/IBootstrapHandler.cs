namespace ZeroX2C.Blog.API.CrossCutting.Bootstrap;

public interface IBootstrapHandler
{
    int Order { get; }

    Task BootstrapAsync(CancellationToken cancellationToken);
}
