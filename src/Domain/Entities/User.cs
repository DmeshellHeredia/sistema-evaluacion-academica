namespace SistemaEvaluacionAcademica.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public Guid RoleId { get; private set; }

    public virtual Role Role { get; private set; } = null!;
    public virtual Student? Student { get; private set; }

    private User() { }

    public User(string email, string passwordHash, string firstName, string lastName, Guid roleId)
    {
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        RoleId = roleId;
    }

    public string FullName => $"{FirstName} {LastName}";

    public Guid SecurityStamp { get; private set; } = Guid.NewGuid();

    // Rotates SecurityStamp so every outstanding JWT (which embeds the stamp as
    // the "sec_stamp" claim) is rejected by OnTokenValidated on the next request,
    // effectively invalidating all active sessions the moment the password changes.
    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        SecurityStamp = Guid.NewGuid();
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
}
