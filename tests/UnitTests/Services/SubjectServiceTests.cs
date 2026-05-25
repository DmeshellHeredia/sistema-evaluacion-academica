using FluentAssertions;
using Moq;
using SistemaEvaluacionAcademica.Application.Services;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;
using SistemaEvaluacionAcademica.UnitTests.Helpers;

namespace SistemaEvaluacionAcademica.UnitTests.Services;

public class SubjectServiceTests
{
    private readonly Mock<IUnitOfWork>        _uow         = new();
    private readonly Mock<ISubjectRepository> _subjectRepo = new();
    private readonly SubjectService           _sut;

    public SubjectServiceTests()
    {
        _uow.Setup(u => u.Subjects).Returns(_subjectRepo.Object);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _sut = new SubjectService(_uow.Object);
    }


    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPageAndTotalCount()
    {
        var subjects = Enumerable.Range(1, 3)
            .Select(i => TestDataBuilder.CreateSubject($"IS-MAT{i}", semesterLevel: i))
            .ToList();

        _subjectRepo.Setup(r => r.GetPagedAsync(1, 10, null, default))
            .ReturnsAsync(((IEnumerable<Subject>)subjects, 25));

        var result = await _sut.GetPagedAsync(1, 10, null);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(3);
        result.Data.TotalCount.Should().Be(25);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
        result.Data.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_ClampsPageToMinimum1()
    {
        _subjectRepo.Setup(r => r.GetPagedAsync(1, 10, null, default))
            .ReturnsAsync((Enumerable.Empty<Subject>(), 0));

        // page = 0 should be clamped to 1
        var result = await _sut.GetPagedAsync(0, 10, null);

        result.IsSuccess.Should().BeTrue();
        _subjectRepo.Verify(r => r.GetPagedAsync(1, 10, null, default), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_ClampsPageSizeTo100()
    {
        _subjectRepo.Setup(r => r.GetPagedAsync(1, 100, null, default))
            .ReturnsAsync((Enumerable.Empty<Subject>(), 0));

        // pageSize = 500 should be clamped to 100
        var result = await _sut.GetPagedAsync(1, 500, null);

        result.IsSuccess.Should().BeTrue();
        _subjectRepo.Verify(r => r.GetPagedAsync(1, 100, null, default), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_WithSearch_PassesSearchToRepository()
    {
        const string search = "Matemáticas";
        _subjectRepo.Setup(r => r.GetPagedAsync(1, 20, search, default))
            .ReturnsAsync((Enumerable.Empty<Subject>(), 0));

        var result = await _sut.GetPagedAsync(1, 20, search);

        result.IsSuccess.Should().BeTrue();
        _subjectRepo.Verify(r => r.GetPagedAsync(1, 20, search, default), Times.Once);
    }


    [Fact]
    public async Task GetPrerequisitesAsync_SubjectNotFound_Returns404()
    {
        _subjectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Subject?)null);

        var result = await _sut.GetPrerequisitesAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPrerequisitesAsync_SubjectExists_ReturnsMappedPrereqs()
    {
        var subject  = TestDataBuilder.CreateSubject("IS-PRG2", semesterLevel: 2);
        var prereqSubject = TestDataBuilder.CreateSubject("IS-PRG1", semesterLevel: 1);
        var prereq = new SubjectPrerequisite(subject.Id, prereqSubject.Id);

        _subjectRepo.Setup(r => r.GetByIdAsync(subject.Id, default)).ReturnsAsync(subject);
        _subjectRepo.Setup(r => r.GetByIdAsync(prereqSubject.Id, default)).ReturnsAsync(prereqSubject);
        _uow.Setup(u => u.GetPrerequisitesAsync(subject.Id, default))
            .ReturnsAsync(new[] { prereq });

        var result = await _sut.GetPrerequisitesAsync(subject.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().SubjectId.Should().Be(prereqSubject.Id);
    }


    [Fact]
    public async Task AddPrerequisiteAsync_SameSubject_Returns400()
    {
        var subjectId = Guid.NewGuid();
        var subject = TestDataBuilder.CreateSubject();
        TestDataBuilder.SetProperty(subject, "Id", subjectId);

        _subjectRepo.Setup(r => r.GetByIdAsync(subjectId, default)).ReturnsAsync(subject);

        var result = await _sut.AddPrerequisiteAsync(subjectId, subjectId);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("sí misma");
    }

    [Fact]
    public async Task AddPrerequisiteAsync_SubjectNotFound_Returns404()
    {
        _subjectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Subject?)null);

        var result = await _sut.AddPrerequisiteAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task AddPrerequisiteAsync_Duplicate_Returns400()
    {
        var subject = TestDataBuilder.CreateSubject("IS-PRG2");
        var prereq  = TestDataBuilder.CreateSubject("IS-PRG1");

        _subjectRepo.Setup(r => r.GetByIdAsync(subject.Id, default)).ReturnsAsync(subject);
        _subjectRepo.Setup(r => r.GetByIdAsync(prereq.Id, default)).ReturnsAsync(prereq);
        _uow.Setup(u => u.PrerequisiteExistsAsync(subject.Id, prereq.Id, default)).ReturnsAsync(true);

        var result = await _sut.AddPrerequisiteAsync(subject.Id, prereq.Id);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("ya está asignado");
    }

    [Fact]
    public async Task AddPrerequisiteAsync_DirectCycle_Returns400()
    {
        // Existing: B → A (B requires A). Adding A → B would create A ↔ B cycle.
        var subjectA = TestDataBuilder.CreateSubject("IS-A");
        var subjectB = TestDataBuilder.CreateSubject("IS-B");
        var existingEdge = new SubjectPrerequisite(subjectB.Id, subjectA.Id); // B requires A

        _subjectRepo.Setup(r => r.GetByIdAsync(subjectA.Id, default)).ReturnsAsync(subjectA);
        _subjectRepo.Setup(r => r.GetByIdAsync(subjectB.Id, default)).ReturnsAsync(subjectB);
        _uow.Setup(u => u.PrerequisiteExistsAsync(subjectA.Id, subjectB.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(new[] { existingEdge });

        // Try to add A → B (A requires B), but B already requires A
        var result = await _sut.AddPrerequisiteAsync(subjectA.Id, subjectB.Id);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("ciclo");
    }

    [Fact]
    public async Task AddPrerequisiteAsync_LongerCycle_Returns400()
    {
        // Existing: B → A, C → B. Adding A → C would create A ← B ← C ← A.
        var subjectA = TestDataBuilder.CreateSubject("IS-A");
        var subjectB = TestDataBuilder.CreateSubject("IS-B");
        var subjectC = TestDataBuilder.CreateSubject("IS-C");
        var edgeBRequiresA = new SubjectPrerequisite(subjectB.Id, subjectA.Id);
        var edgeCRequiresB = new SubjectPrerequisite(subjectC.Id, subjectB.Id);

        _subjectRepo.Setup(r => r.GetByIdAsync(subjectA.Id, default)).ReturnsAsync(subjectA);
        _subjectRepo.Setup(r => r.GetByIdAsync(subjectC.Id, default)).ReturnsAsync(subjectC);
        _uow.Setup(u => u.PrerequisiteExistsAsync(subjectA.Id, subjectC.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(new[] { edgeBRequiresA, edgeCRequiresB });

        // Try to add A → C (A requires C), but C → B → A already exists
        var result = await _sut.AddPrerequisiteAsync(subjectA.Id, subjectC.Id);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("ciclo");
    }

    [Fact]
    public async Task AddPrerequisiteAsync_ValidPrereq_Succeeds()
    {
        var subject = TestDataBuilder.CreateSubject("IS-PRG2", semesterLevel: 2);
        var prereq  = TestDataBuilder.CreateSubject("IS-PRG1", semesterLevel: 1);

        _subjectRepo.Setup(r => r.GetByIdAsync(subject.Id, default)).ReturnsAsync(subject);
        _subjectRepo.Setup(r => r.GetByIdAsync(prereq.Id, default)).ReturnsAsync(prereq);
        _uow.Setup(u => u.PrerequisiteExistsAsync(subject.Id, prereq.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _uow.Setup(u => u.AddPrerequisiteAsync(It.IsAny<SubjectPrerequisite>(), default))
            .Returns(Task.CompletedTask);

        var result = await _sut.AddPrerequisiteAsync(subject.Id, prereq.Id);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.Code.Should().Be("IS-PRG1");
        _uow.Verify(u => u.AddPrerequisiteAsync(It.IsAny<SubjectPrerequisite>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }


    [Fact]
    public async Task RemovePrerequisiteAsync_SubjectNotFound_Returns404()
    {
        _subjectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Subject?)null);

        var result = await _sut.RemovePrerequisiteAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task RemovePrerequisiteAsync_PrereqNotFound_Returns404()
    {
        var subject = TestDataBuilder.CreateSubject();
        _subjectRepo.Setup(r => r.GetByIdAsync(subject.Id, default)).ReturnsAsync(subject);
        _uow.Setup(u => u.PrerequisiteExistsAsync(subject.Id, It.IsAny<Guid>(), default)).ReturnsAsync(false);

        var result = await _sut.RemovePrerequisiteAsync(subject.Id, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task RemovePrerequisiteAsync_Valid_Succeeds()
    {
        var subject = TestDataBuilder.CreateSubject("IS-PRG2");
        var prereqId = Guid.NewGuid();
        _subjectRepo.Setup(r => r.GetByIdAsync(subject.Id, default)).ReturnsAsync(subject);
        _uow.Setup(u => u.PrerequisiteExistsAsync(subject.Id, prereqId, default)).ReturnsAsync(true);
        _uow.Setup(u => u.RemovePrerequisiteAsync(subject.Id, prereqId, default)).Returns(Task.CompletedTask);

        var result = await _sut.RemovePrerequisiteAsync(subject.Id, prereqId);

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.RemovePrerequisiteAsync(subject.Id, prereqId, default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }


    [Fact]
    public async Task SetPrerequisitesAsync_SubjectNotFound_Returns404()
    {
        _subjectRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Subject?)null);

        var result = await _sut.SetPrerequisitesAsync(Guid.NewGuid(), new List<Guid>());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task SetPrerequisitesAsync_SelfReference_Returns400()
    {
        var subject = TestDataBuilder.CreateSubject();
        _subjectRepo.Setup(r => r.GetByIdAsync(subject.Id, default)).ReturnsAsync(subject);

        var result = await _sut.SetPrerequisitesAsync(subject.Id, new[] { subject.Id });

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("sí misma");
    }

    [Fact]
    public async Task SetPrerequisitesAsync_DuplicateIds_Returns400()
    {
        var subject = TestDataBuilder.CreateSubject();
        var prereqId = Guid.NewGuid();
        _subjectRepo.Setup(r => r.GetByIdAsync(subject.Id, default)).ReturnsAsync(subject);

        var result = await _sut.SetPrerequisitesAsync(subject.Id, new[] { prereqId, prereqId });

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("duplicados");
    }

    [Fact]
    public async Task SetPrerequisitesAsync_WithCycle_Returns400()
    {
        // Existing graph (outside of subjectA): C → B. Setting A's prereqs to [B, C].
        // Then check: can B reach A? B has no outgoing edges → no. Can C reach A? C→B, B has no edges → no.
        // That won't detect a cycle. Let's use: existing B→A. Set A prereqs = [B]. B→A already means B requires A.
        // Adding A→B would create a cycle. B can reach A via B→A in existing graph.
        var subjectA = TestDataBuilder.CreateSubject("IS-A");
        var subjectB = TestDataBuilder.CreateSubject("IS-B");
        var edgeBRequiresA = new SubjectPrerequisite(subjectB.Id, subjectA.Id);

        _subjectRepo.Setup(r => r.GetByIdAsync(subjectA.Id, default)).ReturnsAsync(subjectA);
        _subjectRepo.Setup(r => r.GetByIdAsync(subjectB.Id, default)).ReturnsAsync(subjectB);
        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(new[] { edgeBRequiresA }); // existing edge B→A (excluding A's old prereqs, which are none)

        var result = await _sut.SetPrerequisitesAsync(subjectA.Id, new[] { subjectB.Id });

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("ciclo");
    }

    [Fact]
    public async Task SetPrerequisitesAsync_ValidList_ReplacesAndSucceeds()
    {
        var subject  = TestDataBuilder.CreateSubject("IS-PRG2");
        var prereq1  = TestDataBuilder.CreateSubject("IS-PRG1");
        var prereq2  = TestDataBuilder.CreateSubject("IS-MAT1");

        _subjectRepo.Setup(r => r.GetByIdAsync(subject.Id, default)).ReturnsAsync(subject);
        _subjectRepo.Setup(r => r.GetByIdAsync(prereq1.Id, default)).ReturnsAsync(prereq1);
        _subjectRepo.Setup(r => r.GetByIdAsync(prereq2.Id, default)).ReturnsAsync(prereq2);
        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _uow.Setup(u => u.RemoveAllPrerequisitesForSubjectAsync(subject.Id, default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.AddPrerequisiteAsync(It.IsAny<SubjectPrerequisite>(), default)).Returns(Task.CompletedTask);

        var result = await _sut.SetPrerequisitesAsync(subject.Id, new[] { prereq1.Id, prereq2.Id });

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        _uow.Verify(u => u.RemoveAllPrerequisitesForSubjectAsync(subject.Id, default), Times.Once);
        _uow.Verify(u => u.AddPrerequisiteAsync(It.IsAny<SubjectPrerequisite>(), default), Times.Exactly(2));
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task SetPrerequisitesAsync_EmptyList_ClearsAllPrereqs()
    {
        var subject = TestDataBuilder.CreateSubject();
        _subjectRepo.Setup(r => r.GetByIdAsync(subject.Id, default)).ReturnsAsync(subject);
        _uow.Setup(u => u.GetAllActivePrerequisitesAsync(default))
            .ReturnsAsync(Enumerable.Empty<SubjectPrerequisite>());
        _uow.Setup(u => u.RemoveAllPrerequisitesForSubjectAsync(subject.Id, default)).Returns(Task.CompletedTask);

        var result = await _sut.SetPrerequisitesAsync(subject.Id, new List<Guid>());

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
        _uow.Verify(u => u.RemoveAllPrerequisitesForSubjectAsync(subject.Id, default), Times.Once);
        _uow.Verify(u => u.AddPrerequisiteAsync(It.IsAny<SubjectPrerequisite>(), default), Times.Never);
    }
}
