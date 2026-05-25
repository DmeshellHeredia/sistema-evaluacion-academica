using FluentAssertions;
using Moq;
using SistemaEvaluacionAcademica.Application.DTOs.Professors;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Application.Services;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.UnitTests.Helpers;

namespace SistemaEvaluacionAcademica.UnitTests.Services;

public class ProfessorServiceTests
{
    private static readonly Guid ProfesorRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private readonly Mock<IUnitOfWork>      _uow            = new();
    private readonly Mock<IUserRepository>  _userRepo       = new();
    private readonly Mock<IRoleRepository>  _roleRepo       = new();
    private readonly Mock<IPasswordHasher>  _passwordHasher = new();
    private readonly ProfessorService       _sut;

    public ProfessorServiceTests()
    {
        var professorRole = new Role(RoleType.Profesor, "Profesor", "");
        TestDataBuilder.SetProperty(professorRole, "Id", ProfesorRoleId);

        _uow.Setup(u => u.Users).Returns(_userRepo.Object);
        _uow.Setup(u => u.Roles).Returns(_roleRepo.Object);
        _roleRepo.Setup(r => r.GetByTypeAsync(RoleType.Profesor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(professorRole);

        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_pass");
        _sut = new ProfessorService(_uow.Object, _passwordHasher.Object);
    }


    [Fact]
    public async Task GetAllAsync_ShouldReturnAllProfessors()
    {
        var prof1 = TestDataBuilder.CreateUserWithRole("prof1@academia.com", ProfesorRoleId, "Ana", "García");
        var prof2 = TestDataBuilder.CreateUserWithRole("prof2@academia.com", ProfesorRoleId, "Roberto", "López");

        _userRepo
            .Setup(r => r.GetPagedByRoleAsync(ProfesorRoleId, 1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { prof1, prof2 }, 2));

        _uow.Setup(u => u.GetSectionCountsByProfessorsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetAllAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldIncludeSectionCount()
    {
        var prof = TestDataBuilder.CreateUserWithRole("prof@academia.com", ProfesorRoleId, "Ana", "García");

        _userRepo
            .Setup(r => r.GetPagedByRoleAsync(ProfesorRoleId, 1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { prof }, 1));

        _uow.Setup(u => u.GetSectionCountsByProfessorsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [prof.Id] = 2 });

        var result = await _sut.GetAllAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.First().SectionCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_Page2_ShouldReturnSecondPageItems()
    {
        var prof3 = TestDataBuilder.CreateUserWithRole("prof3@academia.com", ProfesorRoleId, "Sofía", "Martínez");

        _userRepo
            .Setup(r => r.GetPagedByRoleAsync(ProfesorRoleId, 2, 2, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { prof3 }, 3));

        _uow.Setup(u => u.GetSectionCountsByProfessorsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetAllAsync(2, 2);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Page.Should().Be(2);
        result.Data.TotalCount.Should().Be(3);
        result.Data.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_ShouldDelegateSearchToRepository()
    {
        var prof = TestDataBuilder.CreateUserWithRole("garcia@academia.com", ProfesorRoleId, "Ana", "García");

        _userRepo
            .Setup(r => r.GetPagedByRoleAsync(ProfesorRoleId, 1, 10, "garcia", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { prof }, 1));

        _uow.Setup(u => u.GetSectionCountsByProfessorsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetAllAsync(1, 10, "garcia");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Email.Should().Be("garcia@academia.com");
        _userRepo.Verify(r => r.GetPagedByRoleAsync(
            ProfesorRoleId, 1, 10, "garcia", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithNoMatchingSearch_ShouldReturnEmptyItems()
    {
        _userRepo
            .Setup(r => r.GetPagedByRoleAsync(ProfesorRoleId, 1, 10, "xqznotexists", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<User>(), 0));

        _uow.Setup(u => u.GetSectionCountsByProfessorsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetAllAsync(1, 10, "xqznotexists");

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAllAsync_WithPage0_ShouldClampToPage1()
    {
        _userRepo
            .Setup(r => r.GetPagedByRoleAsync(ProfesorRoleId, 1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<User>(), 0));

        _uow.Setup(u => u.GetSectionCountsByProfessorsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetAllAsync(0, 10);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Page.Should().Be(1);
        _userRepo.Verify(r => r.GetPagedByRoleAsync(
            ProfesorRoleId, 1, 10, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithPageSize0_ShouldClampTo1()
    {
        _userRepo
            .Setup(r => r.GetPagedByRoleAsync(ProfesorRoleId, 1, 1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<User>(), 0));

        _uow.Setup(u => u.GetSectionCountsByProfessorsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetAllAsync(1, 0);

        result.IsSuccess.Should().BeTrue();
        result.Data!.PageSize.Should().Be(1);
        _userRepo.Verify(r => r.GetPagedByRoleAsync(
            ProfesorRoleId, 1, 1, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithPageSizeOver100_ShouldClampTo100()
    {
        _userRepo
            .Setup(r => r.GetPagedByRoleAsync(ProfesorRoleId, 1, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<User>(), 0));

        _uow.Setup(u => u.GetSectionCountsByProfessorsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _sut.GetAllAsync(1, 200);

        result.IsSuccess.Should().BeTrue();
        result.Data!.PageSize.Should().Be(100);
        _userRepo.Verify(r => r.GetPagedByRoleAsync(
            ProfesorRoleId, 1, 100, null, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnProfessor()
    {
        var prof = TestDataBuilder.CreateUserWithRole("prof@academia.com", ProfesorRoleId, "Ana", "García");
        TestDataBuilder.SetProperty(prof, "RoleId", ProfesorRoleId);

        _userRepo
            .Setup(r => r.GetByIdWithRoleAsync(prof.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prof);

        _uow.Setup(u => u.GetSectionsByProfessorAsync(prof.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SubjectSection>());

        var result = await _sut.GetByIdAsync(prof.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Email.Should().Be("prof@academia.com");
        result.Data.FullName.Should().Be("Ana García");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldReturn404()
    {
        _userRepo
            .Setup(r => r.GetByIdWithRoleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }


    [Fact]
    public async Task CreateAsync_WithUniqueEmail_ShouldCreateProfessor()
    {
        var dto = new CreateProfessorDto("nuevo@academia.com", "Password123!", "Luis", "Martínez");

        _userRepo
            .Setup(r => r.EmailExistsAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepo
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.Email.Should().Be("nuevo@academia.com");
        result.Data.FullName.Should().Be("Luis Martínez");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldReturnFailure()
    {
        var dto = new CreateProfessorDto("dup@academia.com", "Password123!", "Ana", "García");

        _userRepo
            .Setup(r => r.EmailExistsAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task UpdateAsync_WhenExists_ShouldUpdateNameFields()
    {
        var prof = TestDataBuilder.CreateUserWithRole("prof@academia.com", ProfesorRoleId, "Ana", "García");
        var dto = new UpdateProfessorDto("Ana María", "García Torres");

        _userRepo
            .Setup(r => r.GetByIdAsync(prof.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prof);

        _userRepo
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _uow.Setup(u => u.GetSectionsByProfessorAsync(prof.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SubjectSection>());

        var result = await _sut.UpdateAsync(prof.Id, dto);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FirstName.Should().Be("Ana María");
        result.Data.LastName.Should().Be("García Torres");
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ShouldReturn404()
    {
        _userRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateProfessorDto("X", "Y"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }


    [Fact]
    public async Task DeleteAsync_WhenExists_ShouldSoftDeleteUser()
    {
        var prof = TestDataBuilder.CreateUserWithRole("prof@academia.com", ProfesorRoleId, "Ana", "García");

        _userRepo
            .Setup(r => r.GetByIdAsync(prof.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prof);

        _userRepo
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _sut.DeleteAsync(prof.Id);

        result.IsSuccess.Should().BeTrue();
        prof.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ShouldReturn404()
    {
        _userRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
