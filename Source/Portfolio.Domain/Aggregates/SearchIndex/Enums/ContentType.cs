using Portfolio.Common.Seedwork.Enums;

namespace Portfolio.Domain.Aggregates.SearchIndex.Enums;

public sealed class ContentType : SmartEnum<ContentType>
{
    public static readonly ContentType Page  = new(1, "page");
    public static readonly ContentType Image = new(2, "image");
    public static readonly ContentType Video = new(3, "video");
    public static readonly ContentType News  = new(4, "news");

    private ContentType(int id, string name) : base(id, name) { }
}
