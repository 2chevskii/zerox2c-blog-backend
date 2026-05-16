using ZeroX2C.Blog.API.Modules.Shared;

namespace ZeroX2C.Blog.API.Modules.Assets.Images;

public class Image : EntityBase
{
    public required string OriginalFileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public ImagePurpose Purpose { get; set; }
    public required byte[] Content { get; set; }
}
