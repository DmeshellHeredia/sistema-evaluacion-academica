using FluentAssertions;
using Moq;
using SistemaEvaluacionAcademica.Application.DTOs.Periods;
using SistemaEvaluacionAcademica.Application.Services;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;

namespace SistemaEvaluacionAcademica.UnitTests.Services;

public class AcademicPeriodServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly AcademicPeriodService _sut;

    public AcademicPeriodServiceTests()
    {
        _sut = new AcademicPeriodService(_uow.Object);
    }

    private static AcademicPeriod MakePeriod(string name = "2025-1", string code = "2025-1") =>
        new(name, code, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc));


    [Fact]
    public async Task GetByIdAsync_WhenNotFound_Returns404()
    {
        _uow.Setup(u => u.GetPeriodByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicPeriod?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }


    [Fact]
    public async Task CreateAsync_WhenEndDateBeforeStartDate_ReturnsFailure()
    {
        var dto = new CreateAcademicPeriodDto("2025-1", "2025-1",
            new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithValidDates_Succeeds()
    {
        var dto = new CreateAcademicPeriodDto("2025-1", "2025-1",
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc));

        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        _uow.Verify(u => u.AddPeriodAsync(It.IsAny<AcademicPeriod>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task OpenEnrollmentAsync_WhenPeriodNotFound_Returns404()
    {
        _uow.Setup(u => u.GetPeriodByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicPeriod?)null);

        var result = await _sut.OpenEnrollmentAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        _uow.Verify(u => u.CloseAllPeriodsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OpenEnrollmentAsync_CallsCloseAllPeriodsBeforeOpening()
    {
        var period = MakePeriod();
        var closeCallOrder = new List<string>();

        _uow.Setup(u => u.GetPeriodByIdAsync(period.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);
        _uow.Setup(u => u.CloseAllPeriodsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => closeCallOrder.Add("close"))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => closeCallOrder.Add("save"))
            .ReturnsAsync(1);

        var result = await _sut.OpenEnrollmentAsync(period.Id);

        result.IsSuccess.Should().BeTrue();
        closeCallOrder.Should().ContainInOrder("close", "save");
        _uow.Verify(u => u.CloseAllPeriodsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenEnrollmentAsync_SetsIsEnrollmentOpenTrue()
    {
        var period = MakePeriod();
        period.IsEnrollmentOpen.Should().BeFalse();

        _uow.Setup(u => u.GetPeriodByIdAsync(period.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);
        _uow.Setup(u => u.CloseAllPeriodsAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.OpenEnrollmentAsync(period.Id);

        period.IsEnrollmentOpen.Should().BeTrue();
    }


    [Fact]
    public async Task CloseEnrollmentAsync_WhenPeriodNotFound_Returns404()
    {
        _uow.Setup(u => u.GetPeriodByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicPeriod?)null);

        var result = await _sut.CloseEnrollmentAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CloseEnrollmentAsync_WhenOpen_ClosesAndSaves()
    {
        var period = MakePeriod();
        period.OpenEnrollment();
        period.IsEnrollmentOpen.Should().BeTrue();

        _uow.Setup(u => u.GetPeriodByIdAsync(period.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CloseEnrollmentAsync(period.Id);

        result.IsSuccess.Should().BeTrue();
        period.IsEnrollmentOpen.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task DeleteAsync_WhenEnrollmentOpen_ReturnsFailure()
    {
        var period = MakePeriod();
        period.OpenEnrollment();

        _uow.Setup(u => u.GetPeriodByIdAsync(period.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);

        var result = await _sut.DeleteAsync(period.Id);

        result.IsSuccess.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
