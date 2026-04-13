using Portfolio.Common.Seedwork.Enums;

namespace Portfolio.Domain.Aggregates.Sitemap.Enums;

public sealed class ChangeFrequency : SmartEnum<ChangeFrequency>
{
    public static readonly ChangeFrequency Always  = new(1, "always");
    public static readonly ChangeFrequency Hourly  = new(2, "hourly");
    public static readonly ChangeFrequency Daily   = new(3, "daily");
    public static readonly ChangeFrequency Weekly  = new(4, "weekly");
    public static readonly ChangeFrequency Monthly = new(5, "monthly");
    public static readonly ChangeFrequency Yearly  = new(6, "yearly");
    public static readonly ChangeFrequency Never   = new(7, "never");

    private ChangeFrequency(int id, string name) : base(id, name) { }
}
