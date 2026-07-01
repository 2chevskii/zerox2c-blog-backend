using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ZeroX2C.Blog.API.Persistence;

public sealed class BlogDbContextFactory : IDesignTimeDbContextFactory<BlogDbContext>
{
    public BlogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseNpgsql(
                "Host=127.0.0.1;Port=5432;Database=dev;Username=postgres;Password=postgrespassword;",
                npgsql => { }
            )
            .Options;

        return new BlogDbContext(options);
    }
}
