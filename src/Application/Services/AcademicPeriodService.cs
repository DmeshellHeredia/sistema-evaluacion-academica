using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Periods;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;

namespace SistemaEvaluacionAcademica.Application.Services;

public class AcademicPeriodService : IAcademicPeriodService
{
    private readonly IUnitOfWork _unitOfWork;

    public AcademicPeriodService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<AcademicPeriodDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var periods = await _unitOfWork.GetAllPeriodsAsync(cancellationToken);
        return Result<IEnumerable<AcademicPeriodDto>>.Success(periods.Select(MapToDto));
    }

    public async Task<Result<AcademicPeriodDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var period = await _unitOfWork.GetPeriodByIdAsync(id, cancellationToken);
        if (period is null)
            return Result<AcademicPeriodDto>.NotFound("Período académico no encontrado.");
        return Result<AcademicPeriodDto>.Success(MapToDto(period));
    }

    public async Task<Result<AcademicPeriodDto>> CreateAsync(
        CreateAcademicPeriodDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.EndDate <= dto.StartDate)
            return Result<AcademicPeriodDto>.Failure("La fecha de fin debe ser posterior a la fecha de inicio.");

        var period = new AcademicPeriod(dto.Name, dto.Code, dto.StartDate, dto.EndDate);
        await _unitOfWork.AddPeriodAsync(period, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AcademicPeriodDto>.Success(MapToDto(period));
    }

    public async Task<Result<AcademicPeriodDto>> UpdateAsync(
        Guid id, UpdateAcademicPeriodDto dto, CancellationToken cancellationToken = default)
    {
        var period = await _unitOfWork.GetPeriodByIdAsync(id, cancellationToken);
        if (period is null)
            return Result<AcademicPeriodDto>.NotFound("Período académico no encontrado.");

        if (dto.EndDate <= dto.StartDate)
            return Result<AcademicPeriodDto>.Failure("La fecha de fin debe ser posterior a la fecha de inicio.");

        period.Update(dto.Name, dto.Code, dto.StartDate, dto.EndDate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AcademicPeriodDto>.Success(MapToDto(period));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var period = await _unitOfWork.GetPeriodByIdAsync(id, cancellationToken);
        if (period is null)
            return Result<bool>.NotFound("Período académico no encontrado.");

        if (period.IsEnrollmentOpen)
            return Result<bool>.Failure("No se puede eliminar un período con selección de materias activa.");

        await _unitOfWork.RemovePeriodAsync(id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    public async Task<Result<AcademicPeriodDto>> OpenEnrollmentAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var period = await _unitOfWork.GetPeriodByIdAsync(id, cancellationToken);
        if (period is null)
            return Result<AcademicPeriodDto>.NotFound("Período académico no encontrado.");

        // Close any currently open periods first
        await _unitOfWork.CloseAllPeriodsAsync(cancellationToken);

        period.OpenEnrollment();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AcademicPeriodDto>.Success(MapToDto(period));
    }

    public async Task<Result<AcademicPeriodDto>> CloseEnrollmentAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var period = await _unitOfWork.GetPeriodByIdAsync(id, cancellationToken);
        if (period is null)
            return Result<AcademicPeriodDto>.NotFound("Período académico no encontrado.");

        period.CloseEnrollment();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AcademicPeriodDto>.Success(MapToDto(period));
    }

    public async Task<Result<EnrollmentStatusDto>> GetEnrollmentStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var active = await _unitOfWork.GetActivePeriodAsync(cancellationToken);
        var dto = new EnrollmentStatusDto(
            active is not null,
            active?.Name,
            active?.Code,
            active?.StartDate,
            active?.EndDate
        );
        return Result<EnrollmentStatusDto>.Success(dto);
    }

    private static AcademicPeriodDto MapToDto(AcademicPeriod p) =>
        new(p.Id, p.Name, p.Code, p.StartDate, p.EndDate, p.IsEnrollmentOpen, p.CreatedAt);
}
