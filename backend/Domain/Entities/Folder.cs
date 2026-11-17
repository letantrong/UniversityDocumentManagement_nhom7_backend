using Domain.Common;

namespace Domain.Entities;

public class Folder : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
