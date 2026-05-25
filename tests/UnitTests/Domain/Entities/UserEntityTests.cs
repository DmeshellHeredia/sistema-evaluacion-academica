using FluentAssertions;
using SistemaEvaluacionAcademica.Domain.Entities;

namespace SistemaEvaluacionAcademica.UnitTests.Domain.Entities;

public class UserEntityTests
{
    [Fact]
    public void Constructor_ShouldInitializeSecurityStampAsNonEmptyGuid()
    {
        var user = new User("u@test.com", "hash", "Test", "User", Guid.NewGuid());

        user.SecurityStamp.Should().NotBeEmpty();
    }

    [Fact]
    public void UpdatePassword_ShouldChangePasswordHash()
    {
        var user = new User("u@test.com", "oldHash", "Test", "User", Guid.NewGuid());

        user.UpdatePassword("newHash");

        user.PasswordHash.Should().Be("newHash");
    }

    [Fact]
    public void UpdatePassword_ShouldRotateSecurityStamp()
    {
        var user        = new User("u@test.com", "oldHash", "Test", "User", Guid.NewGuid());
        var stampBefore = user.SecurityStamp;

        user.UpdatePassword("newHash");

        user.SecurityStamp.Should().NotBe(stampBefore);
        user.SecurityStamp.Should().NotBeEmpty();
    }

    [Fact]
    public void UpdatePassword_ShouldNotChangeEmailOrName()
    {
        var user = new User("u@test.com", "oldHash", "Test", "User", Guid.NewGuid());

        user.UpdatePassword("newHash");

        user.Email.Should().Be("u@test.com");
        user.FirstName.Should().Be("Test");
        user.LastName.Should().Be("User");
    }
}
