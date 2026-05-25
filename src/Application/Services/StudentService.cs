using SistemaEvaluacionAcademica.Application.Common;
using SistemaEvaluacionAcademica.Application.DTOs.Grades;
using SistemaEvaluacionAcademica.Application.DTOs.Students;
using static SistemaEvaluacionAcademica.Application.Common.EmailHelper;
using SistemaEvaluacionAcademica.Application.Interfaces;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.Domain.Interfaces;

namespace SistemaEvaluacionAcademica.Application.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public StudentService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<PagedResult<StudentDto>>> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (students, totalCount) = await _unitOfWork.Students.GetPagedAsync(page, pageSize, search, cancellationToken);
        var dtos = students.Select(MapToDto);
        var paged = PagedResult<StudentDto>.Create(dtos, totalCount, page, pageSize);
        return Result<PagedResult<StudentDto>>.Success(paged);
    }

    public async Task<Result<StudentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var student = await _unitOfWork.Students.GetWithGradesAsync(id, cancellationToken);
        if (student is null)
            return Result<StudentDto>.NotFound("Estudiante no encontrado.");

        return Result<StudentDto>.Success(MapToDto(student));
    }

    public async Task<Result<StudentDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var student = await _unitOfWork.Students.GetByUserIdAsync(userId, cancellationToken);
        if (student is null)
            return Result<StudentDto>.NotFound("Estudiante no encontrado para este usuario.");

        return Result<StudentDto>.Success(MapToDto(student));
    }

    public async Task<Result<StudentDto>> CreateAsync(CreateStudentDto dto, CancellationToken cancellationToken = default)
    {
        var derivedEmail = DeriveStudentEmail(dto.FirstName, dto.LastName);

        if (await _unitOfWork.Users.EmailExistsAsync(derivedEmail, cancellationToken))
            return Result<StudentDto>.Failure("El correo electrónico ya está registrado.");

        var passwordHash = _passwordHasher.Hash(dto.Password);
        var studentRole = await _unitOfWork.Roles.GetByTypeAsync(RoleType.Estudiante, cancellationToken)
            ?? throw new InvalidOperationException("El rol 'Estudiante' no existe en la base de datos.");

        var user = new User(derivedEmail, passwordHash, dto.FirstName, dto.LastName, studentRole.Id);
        await _unitOfWork.Users.AddAsync(user, cancellationToken);

        var studentCode = Student.GenerateCode();

        var student = new Student(studentCode, user.Id, dto.Career, dto.Semester);
        await _unitOfWork.Students.AddAsync(student, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StudentDto>.Success(MapToDto(student), 201);
    }

    public async Task<Result<StudentDto>> UpdateAsync(Guid id, UpdateStudentDto dto, CancellationToken cancellationToken = default)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id, cancellationToken);
        if (student is null)
            return Result<StudentDto>.NotFound("Estudiante no encontrado.");

        student.Update(dto.Career, dto.Semester);
        await _unitOfWork.Students.UpdateAsync(student, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StudentDto>.Success(MapToDto(student));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(id, cancellationToken);
        if (student is null)
            return Result<bool>.NotFound("Estudiante no encontrado.");

        student.Deactivate();
        await _unitOfWork.Students.UpdateAsync(student, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<StudentGradeReportDto>> GetGradeReportAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _unitOfWork.Students.GetWithGradesAsync(studentId, cancellationToken);
        if (student is null)
            return Result<StudentGradeReportDto>.NotFound("Estudiante no encontrado.");

        var grades = await _unitOfWork.Grades.GetByStudentWithDetailsAsync(studentId, cancellationToken);
        var average = student.CalculateOverallAverage();

        var averageCategory = average.HasValue ? Grade.CategoryFor(average.Value) : "Sin calificaciones";

        var subjectGrades = grades.Select(g => new SubjectGradeDto(
            g.Subject.Code,
            g.Subject.Name,
            g.Value,
            g.CategoryDescription,
            g.Period,
            g.Comments
        ));

        var report = new StudentGradeReportDto(
            student.Id,
            student.StudentCode,
            student.User.FullName,
            student.Career,
            student.Semester,
            average,
            averageCategory,
            subjectGrades
        );

        return Result<StudentGradeReportDto>.Success(report);
    }

    private static StudentDto MapToDto(Student student)
    {
        var average = student.CalculateOverallAverage();
        var category = average.HasValue ? Grade.CategoryFor(average.Value) : "Sin calificaciones";

        return new StudentDto(
            student.Id,
            student.StudentCode,
            student.User?.FullName ?? "Sin nombre",
            student.User?.Email ?? "Sin email",
            student.Career,
            student.Semester,
            average,
            category,
            student.CreatedAt
        );
    }
}
