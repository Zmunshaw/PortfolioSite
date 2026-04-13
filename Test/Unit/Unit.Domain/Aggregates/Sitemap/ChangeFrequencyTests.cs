using Portfolio.Domain.Aggregates.Sitemap.Enums;

namespace Unit.Domain.Aggregates.Sitemap;

public class ChangeFrequencyTests
{
    [Fact]
    public void GetAll_ReturnsSeven()
    {
        var all = ChangeFrequency.GetAll();
        Assert.Equal(7, all.Count);
    }

    [Theory]
    [InlineData(1, "always")]
    [InlineData(2, "hourly")]
    [InlineData(3, "daily")]
    [InlineData(4, "weekly")]
    [InlineData(5, "monthly")]
    [InlineData(6, "yearly")]
    [InlineData(7, "never")]
    public void FromId_Valid_ReturnsExpected(int id, string expectedName)
    {
        var freq = ChangeFrequency.FromId(id);
        Assert.Equal(expectedName, freq.Name);
    }

    [Fact]
    public void FromId_Invalid_Throws()
    {
        Assert.ThrowsAny<Exception>(() => ChangeFrequency.FromId(99));
    }

    [Theory]
    [InlineData("always", 1)]
    [InlineData("hourly", 2)]
    [InlineData("daily", 3)]
    public void FromName_Valid_ReturnsExpected(string name, int expectedId)
    {
        var freq = ChangeFrequency.FromName(name);
        Assert.Equal(expectedId, freq.Id);
    }

    [Fact]
    public void FromName_Invalid_Throws()
    {
        Assert.ThrowsAny<Exception>(() => ChangeFrequency.FromName("biweekly"));
    }

    [Fact]
    public void TryFromId_Valid_ReturnsTrue()
    {
        Assert.True(ChangeFrequency.TryFromId(1, out var freq));
        Assert.Equal(ChangeFrequency.Always, freq);
    }

    [Fact]
    public void TryFromId_Invalid_ReturnsFalse()
    {
        Assert.False(ChangeFrequency.TryFromId(99, out _));
    }
}
