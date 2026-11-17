using Domain.Common;

namespace Domain.Entities;

public class DocumentCategory : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
