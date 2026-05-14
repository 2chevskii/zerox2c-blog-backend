using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ZeroX2C.Blog.API.Persistence;

public sealed class BlogDbContextFactory : IDesignTimeDbContextFactory<BlogDbContext>
{
    public BlogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseMySql(
                "Server=localhost;Port=3306;Database=zerox2c_blog;User=root;Password=password;",
                new MySqlServerVersion(new Version(8, 0, 36))
            )
            .Options;

        return new BlogDbContext(options);
    }
}
