using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SistemaEvaluacionAcademica.Application.DTOs.Auth;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Application.Services;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.UnitTests.Helpers;

namespace SistemaEvaluacionAcademica.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork>        _uow            = new();
    private readonly Mock<IJwtService>        _jwtService     = new();
    private readonly Mock<IPasswordHasher>    _passwordHasher = new();
    private readonly Mock<IUserRepository>    _userRepo       = new();
    private readonly Mock<IStudentRepository> _studentRepo    = new();
    private readonly Mock<IRoleRepository>    _roleRepo       = new();
    private readonly AuthService              _sut;

    private const string PlainPassword  = "TestPass123!";
    private const string HashedPassword = "hashed_TestPass123!";

    public AuthServiceTests()
    {
        _uow.Setup(u => u.Users).Returns(_userRepo.Object);
        _uow.Setup(u => u.Students).Returns(_studentRepo.Object);
        _uow.Setup(u => u.Roles).Returns(_roleRepo.Object);

        // Verify returns true only for the known plain+hash pair; false by default for everything else
        _passwordHasher
            .Setup(h => h.Verify(PlainPassword, HashedPassword))
            .Returns(true);

        _sut = new AuthService(_uow.Object, _jwtService.Object, _passwordHasher.Object, NullLogger<AuthService>.Instance);
    }


    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        var user = BuildActiveUserWithHash();

        _userRepo.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _jwtService.Setup(j => j.GenerateToken(user)).Returns("mocked.jwt.token");
        _jwtService.Setup(j => j.GetTokenExpiration()).Returns(DateTime.UtcNow.AddHours(1));

        var result = await _sut.LoginAsync(new LoginRequest(user.Email, PlainPassword));

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be("mocked.jwt.token");
        result.Data.Email.Should().Be(user.Email);
        result.Data.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ShouldReturnUnauthorized()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync(new LoginRequest("unknown@test.com", "any"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.ErrorMessage.Should().Contain("Credenciales");
        _jwtService.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsWrong_ShouldReturnUnauthorized()
    {
        var user = BuildActiveUserWithHash();

        _userRepo.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        // Moq returns false by default for Verify("WrongPassword!", HashedPassword) — no setup needed

        var result = await _sut.LoginAsync(new LoginRequest(user.Email, "WrongPassword!"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        _jwtService.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsInactive_ShouldReturnUnauthorized()
    {
        // Inactive users are filtered at the repository level (IsActive = false is excluded
        // by GetByEmailAsync), so the service receives null and returns the same 401 as for
        // a non-existent user — no information leak about account existence.
        var user = BuildActiveUserWithHash();
        user.Deactivate();

        _userRepo.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync(new LoginRequest(user.Email, PlainPassword));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        _jwtService.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldCallGetTokenExpiration()
    {
        var user = BuildActiveUserWithHash();
        var expiration = DateTime.UtcNow.AddHours(1);

        _userRepo.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _jwtService.Setup(j => j.GenerateToken(user)).Returns("token");
        _jwtService.Setup(j => j.GetTokenExpiration()).Returns(expiration);

        var result = await _sut.LoginAsync(new LoginRequest(user.Email, PlainPassword));

        result.Data!.ExpiresAt.Should().Be(expiration);
        _jwtService.Verify(j => j.GetTokenExpiration(), Times.Once);
    }


    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldReturnFailure()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("dup@test.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var request = new RegisterRequest("dup@test.com", "Pass123!", "X", "Y", RoleType.Admin, null, null);
        var result  = await _sut.RegisterAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("correo");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenRoleNotFoundInDatabase_ShouldReturnFailure()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _roleRepo.Setup(r => r.GetByTypeAsync(It.IsAny<RoleType>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Role?)null);

        var request = new RegisterRequest("x@y.com", "Admin123!", "X", "Y", RoleType.Admin, null, null);
        var result  = await _sut.RegisterAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ForAdminRole_ShouldCreateUserOnly()
    {
        var adminRole = TestDataBuilder.CreateAdminRole();

        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _roleRepo.Setup(r => r.GetByTypeAsync(RoleType.Admin, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(adminRole);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Returns<User, CancellationToken>((u, _) => Task.FromResult(u));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new RegisterRequest("admin2@test.com", "Admin123!", "Ana", "Soto", RoleType.Admin, null, null);
        var result  = await _sut.RegisterAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);

        _userRepo.Verify(r => r.AddAsync(
            It.Is<User>(u => u.Email == "admin2@test.com"),
            It.IsAny<CancellationToken>()), Times.Once);
        _studentRepo.Verify(r => r.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ForEstudianteRole_ShouldCreateUserAndStudent()
    {
        var estudianteRole = TestDataBuilder.CreateEstudianteRole();

        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _roleRepo.Setup(r => r.GetByTypeAsync(RoleType.Estudiante, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(estudianteRole);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Returns<User, CancellationToken>((u, _) => Task.FromResult(u));
        _studentRepo.Setup(r => r.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()))
                    .Returns<Student, CancellationToken>((s, _) => Task.FromResult(s));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new RegisterRequest(
            "est@test.com", "Est123!", "Luis", "Torres",
            RoleType.Estudiante, Career: "Medicina", Semester: 4);

        var result = await _sut.RegisterAsync(request);

        result.IsSuccess.Should().BeTrue();
        _studentRepo.Verify(r => r.AddAsync(
            It.Is<Student>(s => s.Career == "Medicina" && s.Semester == 4),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPasswordBeforeStoring()
    {
        var adminRole = TestDataBuilder.CreateAdminRole();

        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _roleRepo.Setup(r => r.GetByTypeAsync(RoleType.Admin, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(adminRole);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _passwordHasher.Setup(h => h.Hash(PlainPassword)).Returns("mocked_hash");

        User? capturedUser = null;
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Callback<User, CancellationToken>((u, _) => capturedUser = u)
                 .Returns<User, CancellationToken>((u, _) => Task.FromResult(u));

        await _sut.RegisterAsync(new RegisterRequest("x@y.com", PlainPassword, "X", "Y", RoleType.Admin, null, null));

        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().NotBe(PlainPassword);
        capturedUser.PasswordHash.Should().Be("mocked_hash");
        _passwordHasher.Verify(h => h.Hash(PlainPassword), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldUseRoleIdFromDatabase_NotHardcodedGuid()
    {
        var adminRole = TestDataBuilder.CreateAdminRole();

        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _roleRepo.Setup(r => r.GetByTypeAsync(RoleType.Admin, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(adminRole);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        User? capturedUser = null;
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                 .Callback<User, CancellationToken>((u, _) => capturedUser = u)
                 .Returns<User, CancellationToken>((u, _) => Task.FromResult(u));

        await _sut.RegisterAsync(new RegisterRequest("x@y.com", PlainPassword, "X", "Y", RoleType.Admin, null, null));

        capturedUser!.RoleId.Should().Be(adminRole.Id);
        _roleRepo.Verify(r => r.GetByTypeAsync(RoleType.Admin, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task ChangePasswordAsync_WhenUserNotFound_ShouldReturnNotFound()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var result = await _sut.ChangePasswordAsync(Guid.NewGuid(), "old", "new");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenCurrentPasswordIsWrong_ShouldReturnFailure()
    {
        var user = BuildActiveUserWithHash();

        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        // Moq returns false by default for Verify("WrongCurrent!", HashedPassword) — no setup needed

        var result = await _sut.ChangePasswordAsync(user.Id, "WrongCurrent!", "NewPass123!");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("contraseña");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ShouldUpdateAndSave()
    {
        var user = BuildActiveUserWithHash();

        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _passwordHasher.Setup(h => h.Hash("NuevoPass123!")).Returns("hashed_new_pass");

        var result = await _sut.ChangePasswordAsync(user.Id, PlainPassword, "NuevoPass123!");

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("hashed_new_pass");
        _passwordHasher.Verify(h => h.Hash("NuevoPass123!"), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ShouldRotateSecurityStamp()
    {
        // SecurityStamp rotation is what invalidates all outstanding JWTs on the next request.
        // OnTokenValidated compares the "sec_stamp" claim against the current DB value;
        // a new stamp means every pre-change token is rejected immediately.
        var user        = BuildActiveUserWithHash();
        var stampBefore = user.SecurityStamp;

        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.ChangePasswordAsync(user.Id, PlainPassword, "NuevoPass123!");

        result.IsSuccess.Should().BeTrue();
        user.SecurityStamp.Should().NotBe(stampBefore);
        user.SecurityStamp.Should().NotBeEmpty();
    }


    private User BuildActiveUserWithHash()
    {
        var role = TestDataBuilder.CreateAdminRole();
        var user = new User("admin@test.com", HashedPassword, "Admin", "Test", role.Id);
        TestDataBuilder.SetProperty(user, "Role", role);
        return user;
    }
}
