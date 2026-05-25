using Microsoft.Extensions.Logging;
using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Grades;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Constants;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Interfaces;

namespace SistemaEvaluacionAcademica.Application.Services;

public class GradeService : IGradeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GradeService> _logger;

    public GradeService(IUnitOfWork unitOfWork, ILogger<GradeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<GradeDto>> CreateAsync(
        CreateGradeDto dto,
        Guid gradedByUserId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var student = await _unitOfWork.Students.GetWithGradesAsync(dto.StudentId, cancellationToken);
        if (student is null)
            return Result<GradeDto>.NotFound("Estudiante no encontrado.");

        var section = await _unitOfWork.GetSectionByIdAsync(dto.SectionId, cancellationToken);
        if (section is null)
            return Result<GradeDto>.NotFound("Sección no encontrada.");

        if (userRole == Roles.Profesor && section.ProfessorId != gradedByUserId)
        {
            _logger.LogWarning(
                "Acceso denegado: usuario {UserId} intentó calificar en sección {SectionId} sin ser el profesor asignado",
                gradedByUserId, dto.SectionId);
            return Result<GradeDto>.Forbidden("Solo puedes calificar en secciones que tienes asignadas.");
        }

        if (!await _unitOfWork.IsEnrolledInSectionAsync(dto.StudentId, dto.SectionId, cancellationToken))
            return Result<GradeDto>.Failure(
                $"El estudiante no está inscrito en la sección '{section.SectionCode}'.");

        var existing = await _unitOfWork.Grades.GetByStudentSubjectAndPeriodAsync(
            dto.StudentId, section.SubjectId, dto.Period, cancellationToken);

        if (existing is not null)
            return Result<GradeDto>.Failure(
                $"Ya existe una calificación para este estudiante en la materia en el período '{dto.Period}'.");

        var grade = new Grade(dto.StudentId, section.SubjectId, dto.SectionId, dto.Value, dto.Period, gradedByUserId, dto.Comments);
        await _unitOfWork.Grades.AddAsync(grade, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GradeDto>.Success(MapToDto(grade, student, section.Subject), 201);
    }

    public async Task<Result<GradeDto>> UpdateAsync(
        Guid id, decimal newValue, string? comments,
        Guid updatedByUserId, string userRole,
        CancellationToken cancellationToken = default)
    {
        var grade = await _unitOfWork.Grades.GetByIdAsync(id, cancellationToken);
        if (grade is null)
            return Result<GradeDto>.NotFound("Calificación no encontrada.");

        if (userRole == Roles.Profesor)
        {
            if (grade.SectionId is null)
                return Result<GradeDto>.Forbidden("No se puede modificar esta calificación.");

            var section = await _unitOfWork.GetSectionByIdAsync(grade.SectionId.Value, cancellationToken);
            if (section?.ProfessorId != updatedByUserId)
            {
                _logger.LogWarning(
                    "Acceso denegado: usuario {UserId} intentó modificar calificación {GradeId} sin ser el profesor de la sección",
                    updatedByUserId, id);
                return Result<GradeDto>.Forbidden("Solo puedes modificar calificaciones de tus secciones.");
            }
        }

        grade.Update(newValue, comments);
        await _unitOfWork.Grades.UpdateAsync(grade, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var gradeWithDetails = await _unitOfWork.Grades.GetByStudentWithDetailsAsync(grade.StudentId, cancellationToken);
        var updatedGrade = gradeWithDetails.FirstOrDefault(g => g.Id == id);

        return updatedGrade is not null
            ? Result<GradeDto>.Success(MapToDto(updatedGrade, updatedGrade.Student, updatedGrade.Subject))
            : Result<GradeDto>.NotFound("Calificación no encontrada tras actualización.");
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var grade = await _unitOfWork.Grades.GetByIdAsync(id, cancellationToken);
        if (grade is null)
            return Result<bool>.NotFound("Calificación no encontrada.");

        await _unitOfWork.Grades.DeleteAsync(grade, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<PagedResult<GradeDto>>> GetByStudentAsync(
        Guid studentId, int page, int pageSize, string? period,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (grades, totalCount) = await _unitOfWork.Grades.GetPagedByStudentAsync(studentId, page, pageSize, period, cancellationToken);
        var dtos = grades.Select(g => MapToDto(g, g.Student, g.Subject));
        return Result<PagedResult<GradeDto>>.Success(PagedResult<GradeDto>.Create(dtos, totalCount, page, pageSize));
    }

    public async Task<Result<PagedResult<GradeDto>>> GetBySubjectAsync(
        Guid subjectId, string? period, int page, int pageSize,
        Guid requestingUserId, string userRole,
        CancellationToken cancellationToken = default)
    {
        if (userRole == Roles.Profesor)
        {
            var professorSections = await _unitOfWork.GetSectionsByProfessorAsync(requestingUserId, cancellationToken);
            var hasSection = professorSections.Any(s => s.SubjectId == subjectId && s.IsActive);
            if (!hasSection)
                return Result<PagedResult<GradeDto>>.Forbidden("Solo puedes ver calificaciones de materias que impartes.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (grades, totalCount) = await _unitOfWork.Grades.GetPagedBySubjectAsync(subjectId, period, page, pageSize, cancellationToken);
        var dtos = grades.Select(g => MapToDto(g, g.Student, g.Subject));
        return Result<PagedResult<GradeDto>>.Success(PagedResult<GradeDto>.Create(dtos, totalCount, page, pageSize));
    }

    public async Task<Result<decimal?>> GetStudentAverageAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var average = await _unitOfWork.Grades.GetStudentAverageAsync(studentId, cancellationToken);
        return Result<decimal?>.Success(average);
    }

    public async Task<Result<decimal>> GetSubjectAverageAsync(Guid subjectId, string period, CancellationToken cancellationToken = default)
    {
        var average = await _unitOfWork.Grades.GetSubjectAverageAsync(subjectId, period, cancellationToken);
        return Result<decimal>.Success(average);
    }

    public async Task<Result<PagedResult<GradeDto>>> GetBySectionAsync(
        Guid sectionId, int page, int pageSize,
        Guid requestingUserId, string userRole,
        CancellationToken cancellationToken = default)
    {
        var section = await _unitOfWork.GetSectionByIdAsync(sectionId, cancellationToken);
        if (section is null)
            return Result<PagedResult<GradeDto>>.NotFound("Sección no encontrada.");

        if (userRole == Roles.Profesor && section.ProfessorId != requestingUserId)
            return Result<PagedResult<GradeDto>>.Forbidden(
                "Solo puedes ver calificaciones de tus secciones.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (grades, totalCount) = await _unitOfWork.Grades.GetPagedBySectionAsync(sectionId, page, pageSize, cancellationToken);
        return Result<PagedResult<GradeDto>>.Success(
            PagedResult<GradeDto>.Create(grades.Select(g => MapToDto(g, g.Student, g.Subject)), totalCount, page, pageSize));
    }

    private static GradeDto MapToDto(Grade grade, Student student, Subject subject) =>
        new(
            grade.Id,
            student.StudentCode,
            student.User.FullName,
            subject.Code,
            subject.Name,
            grade.Value,
            grade.CategoryDescription,
            grade.Period,
            grade.Comments,
            grade.CreatedAt
        );
}
