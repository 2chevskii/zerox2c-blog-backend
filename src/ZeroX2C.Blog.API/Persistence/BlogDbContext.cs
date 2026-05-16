using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Assets.Images;
using ZeroX2C.Blog.API.Modules.Posts;
using ZeroX2C.Blog.API.Modules.Posts.Tags;
using ZeroX2C.Blog.API.Modules.Users;

namespace ZeroX2C.Blog.API.Persistence;

public class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserExternalLogin> UserExternalLogins { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<PostTag> PostTags { get; set; }
    public DbSet<Image> Images { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Username).HasMaxLength(64).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(user => user.BlockedReason).HasMaxLength(512);
            entity.Property(user => user.CreatedAt).IsRequired();

            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        builder.Entity<UserExternalLogin>(entity =>
        {
            entity.ToTable("UserExternalLogins");
            entity.HasKey(login => login.Id);

            entity.Property(login => login.Provider).HasMaxLength(64).IsRequired();
            entity.Property(login => login.ProviderUserId).HasMaxLength(256).IsRequired();
            entity.Property(login => login.ProviderDisplayName).HasMaxLength(256);

            entity.HasIndex(login => new { login.Provider, login.ProviderUserId }).IsUnique();
            entity.HasIndex(login => login.UserId);

            entity.HasOne(login => login.User)
                .WithMany(user => user.ExternalLogins)
                .HasForeignKey(login => login.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Post>(entity =>
        {
            entity.ToTable("Posts");
            entity.HasKey(post => post.Id);

            entity.Property(post => post.Slug).HasMaxLength(PostSlug.MaxLength);
            entity.Property(post => post.Title).HasMaxLength(256).IsRequired();
            entity.Property(post => post.Subtitle).HasMaxLength(512);
            entity.Property(post => post.Excerpt).HasMaxLength(1000);
            entity.Property(post => post.Body).IsRequired();
            entity.Property(post => post.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(PostStatus.Draft)
                .IsRequired();
            entity.Property(post => post.CreatedAt).IsRequired();

            entity.HasIndex(post => post.Slug).IsUnique();
            entity.HasIndex(post => new { post.Status, post.PublishedAt });
            entity.HasIndex(post => post.CreatedAt);

            entity.HasOne<Image>()
                .WithMany()
                .HasForeignKey(post => post.CoverImageId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<Image>()
                .WithMany()
                .HasForeignKey(post => post.BannerImageId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tags");
            entity.HasKey(tag => tag.Id);

            entity.Property(tag => tag.Name).HasMaxLength(TagName.MaxLength).IsRequired();
            entity.Property(tag => tag.Description).HasMaxLength(512);
            entity.Property(tag => tag.CreatedAt).IsRequired();

            entity.HasIndex(tag => tag.Name).IsUnique();
        });

        builder.Entity<PostTag>(entity =>
        {
            entity.ToTable("PostTags");
            entity.HasKey(postTag => postTag.Id);

            entity.Property(postTag => postTag.CreatedAt).IsRequired();

            entity.HasIndex(postTag => postTag.PostId);
            entity.HasIndex(postTag => postTag.TagId);
            entity.HasIndex(postTag => new { postTag.PostId, postTag.TagId }).IsUnique();

            entity.HasOne(postTag => postTag.Post)
                .WithMany(post => post.PostTags)
                .HasForeignKey(postTag => postTag.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(postTag => postTag.Tag)
                .WithMany(tag => tag.PostTags)
                .HasForeignKey(postTag => postTag.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Image>(entity =>
        {
            entity.ToTable("Images");
            entity.HasKey(image => image.Id);

            entity.Property(image => image.OriginalFileName).HasMaxLength(256).IsRequired();
            entity.Property(image => image.ContentType).HasMaxLength(128).IsRequired();
            entity.Property(image => image.SizeBytes).IsRequired();
            entity.Property(image => image.Purpose)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(image => image.Content).HasColumnType("longblob").IsRequired();
            entity.Property(image => image.CreatedAt).IsRequired();

            entity.HasIndex(image => image.Purpose);
        });
    }
}
