using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Sections;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Constants;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Interfaces;

namespace SistemaEvaluacionAcademica.Application.Services;

public class SectionService : ISectionService
{
    private readonly IUnitOfWork _unitOfWork;

    public SectionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<SectionDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var sections = await _unitOfWork.GetAllSectionsAsync(ct);
        return Result<IEnumerable<SectionDto>>.Success(sections.Select(MapToDto));
    }

    public async Task<Result<PagedResult<SectionDto>>> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await _unitOfWork.GetAllSectionsPagedAsync(page, pageSize, search, ct);
        return Result<PagedResult<SectionDto>>.Success(
            PagedResult<SectionDto>.Create(items.Select(MapToDto), total, page, pageSize));
    }

    public async Task<Result<IReadOnlyList<SectionLookupDto>>> GetLookupAsync(
        string? search = null, int limit = 50, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var raw = await _unitOfWork.GetSectionsLookupAsync(search, limit, ct);
        var dtos = raw.Select(x => new SectionLookupDto(
            x.Id, x.SubjectCode, x.SubjectName, x.SectionCode, x.DayOfWeek,
            x.StartTime.ToString("HH:mm"), x.EndTime.ToString("HH:mm"))).ToList();
        return Result<IReadOnlyList<SectionLookupDto>>.Success(dtos);
    }

    public async Task<Result<IEnumerable<SectionDto>>> GetByProfessorAsync(Guid professorUserId, CancellationToken ct = default)
    {
        var sections = await _unitOfWork.GetSectionsByProfessorAsync(professorUserId, ct);
        return Result<IEnumerable<SectionDto>>.Success(sections.Select(MapToDto));
    }

    public async Task<Result<SectionDto>> GetByIdAsync(Guid sectionId, CancellationToken ct = default)
    {
        var section = await _unitOfWork.GetSectionByIdAsync(sectionId, ct);
        if (section is null)
            return Result<SectionDto>.NotFound("Sección no encontrada.");

        return Result<SectionDto>.Success(MapToDto(section));
    }

    public async Task<Result<SectionDto>> CreateAsync(CreateSectionDto dto, CancellationToken ct = default)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(dto.SubjectId, ct);
        if (subject is null)
            return Result<SectionDto>.NotFound("Materia no encontrada.");

        var professor = await _unitOfWork.Users.GetByIdWithRoleAsync(dto.ProfessorId, ct);
        if (professor is null)
            return Result<SectionDto>.NotFound("Profesor no encontrado.");

        if (professor.Role.Type != RoleType.Profesor && professor.Role.Type != RoleType.Admin)
            return Result<SectionDto>.Failure("El usuario especificado no tiene rol de Profesor.");

        if (!TimeOnly.TryParse(dto.StartTime, out var startTime) ||
            !TimeOnly.TryParse(dto.EndTime, out var endTime))
            return Result<SectionDto>.Failure("Formato de hora inválido. Use HH:mm.");

        if (endTime <= startTime)
            return Result<SectionDto>.Failure("La hora de fin debe ser posterior a la hora de inicio.");

        // Verificar código de sección único dentro de la materia
        var existingSections = await _unitOfWork.GetSectionsBySubjectAsync(dto.SubjectId, ct);
        if (existingSections.Any(s => s.SectionCode == dto.SectionCode))
            return Result<SectionDto>.Failure(
                $"Ya existe una sección con código '{dto.SectionCode}' para esta materia.");

        var section = new SubjectSection(
            dto.SubjectId, dto.ProfessorId, dto.SectionCode,
            dto.DayOfWeek, startTime, endTime, dto.Modality, dto.Capacity, dto.Classroom);

        await _unitOfWork.AddSectionAsync(section, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Recargar con propiedades de navegación
        var created = await _unitOfWork.GetSectionByIdAsync(section.Id, ct);
        return Result<SectionDto>.Success(MapToDto(created!), 201);
    }

    public async Task<Result<SectionDto>> UpdateAsync(Guid sectionId, UpdateSectionDto dto, CancellationToken ct = default)
    {
        var section = await _unitOfWork.GetSectionByIdAsync(sectionId, ct);
        if (section is null)
            return Result<SectionDto>.NotFound("Sección no encontrada.");

        if (!TimeOnly.TryParse(dto.StartTime, out var startTime) ||
            !TimeOnly.TryParse(dto.EndTime, out var endTime))
            return Result<SectionDto>.Failure("Formato de hora inválido. Use HH:mm.");

        if (endTime <= startTime)
            return Result<SectionDto>.Failure("La hora de fin debe ser posterior a la hora de inicio.");

        var enrolledCount = section.ActiveEnrollmentCount;
        if (dto.Capacity < enrolledCount)
            return Result<SectionDto>.Failure(
                $"No se puede reducir la capacidad a {dto.Capacity}. Hay {enrolledCount} estudiante(s) inscritos actualmente.");

        section.Update(dto.DayOfWeek, startTime, endTime, dto.Modality, dto.Capacity, dto.Classroom);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<SectionDto>.Success(MapToDto(section));
    }

    public async Task<Result<bool>> DeleteAsync(Guid sectionId, CancellationToken ct = default)
    {
        var section = await _unitOfWork.GetSectionByIdAsync(sectionId, ct);
        if (section is null)
            return Result<bool>.NotFound("Sección no encontrada.");

        section.Deactivate();
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<SectionStudentDto>>> GetStudentsAsync(
        Guid sectionId, Guid requestingUserId, string userRole, CancellationToken ct = default)
    {
        var section = await _unitOfWork.GetSectionByIdAsync(sectionId, ct);
        if (section is null)
            return Result<IEnumerable<SectionStudentDto>>.NotFound("Sección no encontrada.");

        if (userRole == Roles.Profesor && section.ProfessorId != requestingUserId)
            return Result<IEnumerable<SectionStudentDto>>.Forbidden(
                "Solo puedes ver los estudiantes de tus propias secciones.");

        var grades = await _unitOfWork.Grades.GetBySectionAsync(sectionId, ct);
        var gradeByStudent = grades
            .Where(g => g.IsActive)
            .GroupBy(g => g.StudentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.CreatedAt).First());

        var dtos = section.Enrollments
            .Where(e => e.IsActive)
            .Select(e => {
                var grade = gradeByStudent.GetValueOrDefault(e.StudentId);
                return new SectionStudentDto(
                    e.StudentId,
                    e.Student.StudentCode,
                    e.Student.User.FullName,
                    e.Student.User.Email,
                    grade?.Value,
                    grade?.Id,
                    grade?.Comments
                );
            });

        return Result<IEnumerable<SectionStudentDto>>.Success(dtos);
    }

    private static SectionDto MapToDto(SubjectSection s) =>
        new(
            s.Id,
            s.SubjectId,
            s.Subject.Code,
            s.Subject.Name,
            s.ProfessorId,
            s.Professor is { IsActive: true } p ? p.FullName : "Sin profesor asignado",
            s.SectionCode,
            s.DayOfWeek,
            s.StartTime.ToString("HH:mm"),
            s.EndTime.ToString("HH:mm"),
            s.Modality,
            s.Capacity,
            s.Enrollments.Count(e => e.IsActive),
            s.IsFull,
            s.Classroom
        );
}
