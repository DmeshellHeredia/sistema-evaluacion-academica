namespace SistemaEvaluacionAcademica.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsActive { get; private set; } = true;

    public void Activate()   => IsActive = true;
    public void Deactivate() => IsActive = false;
}
