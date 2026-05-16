using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;
using ZeroX2C.Blog.API.Modules.Posts.Tags;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed class AdminTagService(BlogDbContext dbContext) : IAdminTagService
{
    public async Task<IReadOnlyCollection<AdminTagResponse>> GetTagsAsync(
        int offset,
        int limit,
        string? search,
        CancellationToken cancellationToken
    )
    {
        var query = dbContext.Tags.Where(tag => !tag.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(tag =>
                tag.Name.Contains(normalizedSearch)
                || (tag.Description != null && tag.Description.Contains(normalizedSearch))
            );
        }

        var tags = await query
            .OrderBy(tag => tag.Name)
            .ThenBy(tag => tag.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return tags.Select(TagMapper.ToAdminResponse).ToArray();
    }

    public async Task<AdminTagOperationResult<AdminTagResponse>> GetTagAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var tag = await FindActiveTagAsync(id, cancellationToken);

        return tag is null
            ? AdminTagOperationResult<AdminTagResponse>.Failure(
                AdminTagOperationStatus.TagNotFound
            )
            : AdminTagOperationResult<AdminTagResponse>.Success(TagMapper.ToAdminResponse(tag));
    }

    public async Task<AdminTagOperationResult<AdminTagResponse>> CreateTagAsync(
        CreateTagRequest request,
        CancellationToken cancellationToken
    )
    {
        var nameResult = await NormalizeAndValidateNameAsync(
            request.Name,
            excludedTagId: null,
            cancellationToken
        );
        if (nameResult.Status != AdminTagOperationStatus.Success)
        {
            return AdminTagOperationResult<AdminTagResponse>.Failure(nameResult.Status);
        }

        var tag = new Tag
        {
            Id = Guid.CreateVersion7(),
            Name = nameResult.Value!,
            Description = NormalizeOptionalText(request.Description),
        };

        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminTagOperationResult<AdminTagResponse>.Success(
            TagMapper.ToAdminResponse(tag)
        );
    }

    public async Task<AdminTagOperationResult<AdminTagResponse>> UpdateTagAsync(
        Guid id,
        UpdateTagRequest request,
        CancellationToken cancellationToken
    )
    {
        var tag = await FindActiveTagAsync(id, cancellationToken);
        if (tag is null)
        {
            return AdminTagOperationResult<AdminTagResponse>.Failure(
                AdminTagOperationStatus.TagNotFound
            );
        }

        var nameResult = await NormalizeAndValidateNameAsync(
            request.Name,
            excludedTagId: id,
            cancellationToken
        );
        if (nameResult.Status != AdminTagOperationStatus.Success)
        {
            return AdminTagOperationResult<AdminTagResponse>.Failure(nameResult.Status);
        }

        tag.Name = nameResult.Value!;
        tag.Description = NormalizeOptionalText(request.Description);

        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminTagOperationResult<AdminTagResponse>.Success(
            TagMapper.ToAdminResponse(tag)
        );
    }

    public async Task<AdminTagOperationResult> DeleteTagAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var tag = await dbContext
            .Tags.Include(existingTag => existingTag.PostTags.Where(postTag => !postTag.IsDeleted))
            .SingleOrDefaultAsync(
                existingTag => existingTag.Id == id && !existingTag.IsDeleted,
                cancellationToken
            );
        if (tag is null)
        {
            return AdminTagOperationResult.Failure(AdminTagOperationStatus.TagNotFound);
        }

        dbContext.Tags.Remove(tag);

        foreach (var postTag in tag.PostTags)
        {
            dbContext.PostTags.Remove(postTag);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return AdminTagOperationResult.Success();
    }

    private async Task<TagNameOperationResult> NormalizeAndValidateNameAsync(
        string name,
        Guid? excludedTagId,
        CancellationToken cancellationToken
    )
    {
        var normalizedName = TagName.Normalize(name);
        if (string.IsNullOrWhiteSpace(normalizedName) || !TagName.IsValid(normalizedName))
        {
            return TagNameOperationResult.Failure(AdminTagOperationStatus.InvalidName);
        }

        var isNameTaken = await dbContext.Tags.AnyAsync(
            tag =>
                tag.Name == normalizedName
                && (excludedTagId == null || tag.Id != excludedTagId),
            cancellationToken
        );

        return isNameTaken
            ? TagNameOperationResult.Failure(AdminTagOperationStatus.NameAlreadyTaken)
            : TagNameOperationResult.Success(normalizedName);
    }

    private Task<Tag?> FindActiveTagAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Tags.SingleOrDefaultAsync(
            existingTag => existingTag.Id == id && !existingTag.IsDeleted,
            cancellationToken
        );

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
