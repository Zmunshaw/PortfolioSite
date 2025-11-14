namespace SiteBackend.DTO;

public class DTOUrlData
{
    public int UrlID { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime? LastModified { get; set; }
    public string? ChangeFrequency { get; set; }
    public float Priority { get; set; } = 0.5f;
    public List<DTOMediaData> Media { get; set; } = new();
}
