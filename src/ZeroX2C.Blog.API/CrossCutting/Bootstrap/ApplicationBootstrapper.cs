namespace ZeroX2C.Blog.API.CrossCutting.Bootstrap;

public sealed class ApplicationBootstrapper(IEnumerable<IBootstrapHandler> bootstrapHandlers)
{
    private readonly IReadOnlyList<IBootstrapHandler> _bootstrapHandlers = bootstrapHandlers
        .OrderBy(handler => handler.Order)
        .ToArray();

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        foreach (var bootstrapHandler in _bootstrapHandlers)
        {
            await bootstrapHandler.BootstrapAsync(cancellationToken);
        }
    }
}
