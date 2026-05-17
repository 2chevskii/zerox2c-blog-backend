using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ZeroX2C.Blog.API.Persistence;

public sealed class BlogDbContextFactory : IDesignTimeDbContextFactory<BlogDbContext>
{
    public BlogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseMySql(
                "Server=127.0.0.1;Port=3306;Database=dev;User=root;Password=rootpassword;AllowPublicKeyRetrieval=True;SslMode=None;",
                new MySqlServerVersion(new Version(8, 0, 36))
            )
            .Options;

        return new BlogDbContext(options);
    }
}
