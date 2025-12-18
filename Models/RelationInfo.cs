namespace DataNath.ApiMetadatos.Models;

public class RelationInfo
{
    public string RelationName { get; set; } = string.Empty;
    public string FromTable { get; set; } = string.Empty;
    public string FromColumn { get; set; } = string.Empty;
    public string ToTable { get; set; } = string.Empty;
    public string ToColumn { get; set; } = string.Empty;
    public string RelationType { get; set; } = string.Empty; // "ForeignKey" or "ReferencedBy"
}
