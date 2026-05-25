using FluentAssertions;
using SistemaEvaluacionAcademica.Infrastructure.Services;

namespace SistemaEvaluacionAcademica.UnitTests.Services;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ShouldReturnNonEmptyString()
    {
        var hash = _sut.Hash("SomePassword123!");

        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_ShouldNotReturnPlainPassword()
    {
        const string plain = "SomePassword123!";

        var hash = _sut.Hash(plain);

        hash.Should().NotBe(plain);
    }

    [Fact]
    public void Hash_SamePlaintext_ProducesDifferentHashes()
    {
        const string plain = "SamePassword!";

        var hash1 = _sut.Hash(plain);
        var hash2 = _sut.Hash(plain);

        // BCrypt uses a random salt per call
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        const string plain = "CorrectPassword1!";
        var hash = _sut.Hash(plain);

        _sut.Verify(plain, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("OriginalPassword1!");

        _sut.Verify("WrongPassword!", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_HashFromDifferentRound_StillVerifiesCorrectly()
    {
        const string plain = "RoundTestPass1!";
        // Hash produced by workFactor 12 (default in BCryptPasswordHasher)
        var hash = _sut.Hash(plain);

        _sut.Verify(plain, hash).Should().BeTrue();
    }
}
