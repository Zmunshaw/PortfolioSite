using Portfolio.Common.Seedwork.Enums;

namespace Portfolio.Domain.Aggregates.Sitemap.Enums;

public sealed class ChangeFrequency : SmartEnum<ChangeFrequency>
{
    public static readonly ChangeFrequency Always  = new(1, nameof(Always));
    public static readonly ChangeFrequency Hourly  = new(2, nameof(Hourly));
    public static readonly ChangeFrequency Daily   = new(3, nameof(Daily));
    public static readonly ChangeFrequency Weekly  = new(4, nameof(Weekly));
    public static readonly ChangeFrequency Monthly = new(5, nameof(Monthly));
    public static readonly ChangeFrequency Yearly  = new(6, nameof(Yearly));
    public static readonly ChangeFrequency Never   = new(7, nameof(Never));

    private ChangeFrequency(int id, string name) : base(id, name) { }
}
