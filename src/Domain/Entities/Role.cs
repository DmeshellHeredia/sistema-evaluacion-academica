using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Domain.Entities;

public class Role : BaseEntity
{
    public RoleType Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public virtual ICollection<User> Users { get; private set; } = new List<User>();

    private Role() { }

    public Role(RoleType type, string name, string description)
    {
        Type = type;
        Name = name;
        Description = description;
    }
}
