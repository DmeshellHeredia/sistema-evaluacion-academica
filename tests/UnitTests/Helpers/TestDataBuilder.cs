using System.Reflection;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.UnitTests.Helpers;

public static class TestDataBuilder
{
    public static Role CreateAdminRole()      => new(RoleType.Admin, "Admin", "Administrador del sistema");
    public static Role CreateProfesorRole()   => new(RoleType.Profesor, "Profesor", "Docente del sistema");
    public static Role CreateEstudianteRole() => new(RoleType.Estudiante, "Estudiante", "Alumno del sistema");

    public static User CreateAdminUser(
        string email = "admin@academia.com",
        string passwordHash = "$2a$12$dummyHashForTestingOnly123456",
        string firstName = "Admin",
        string lastName = "Sistema")
    {
        var role = CreateAdminRole();
        var user = new User(email, passwordHash, firstName, lastName, role.Id);
        SetProperty(user, "Role", role);
        return user;
    }

    public static User CreateProfesorUser(
        string email = "prof.garcia@academia.com",
        string firstName = "Carlos",
        string lastName = "García")
    {
        var role = CreateProfesorRole();
        var user = new User(email, "$2a$12$dummyHash", firstName, lastName, role.Id);
        SetProperty(user, "Role", role);
        return user;
    }

    public static User CreateEstudianteUser(
        string email = "juan.perez@academia.com",
        string firstName = "Juan",
        string lastName = "Pérez")
    {
        var role = CreateEstudianteRole();
        var user = new User(email, "$2a$12$dummyHash", firstName, lastName, role.Id);
        SetProperty(user, "Role", role);
        return user;
    }

    public static User CreateUserWithRole(
        string email,
        Guid roleId,
        string firstName = "Test",
        string lastName = "User")
        => new(email, "$2a$12$dummyHash", firstName, lastName, roleId);

    public static Student CreateStudent(
        string studentCode = "EST-2025-0001",
        Guid? userId = null,
        string career = "Ingeniería en Sistemas",
        int semester = 3)
        => new(studentCode, userId ?? Guid.NewGuid(), career, semester);

    public static Student CreateStudentWithUser(
        string studentCode = "EST-2025-0001",
        string email = "juan.perez@academia.com",
        string firstName = "Juan",
        string lastName = "Pérez",
        string career = "Ingeniería en Sistemas",
        int semester = 3)
    {
        var user = CreateEstudianteUser(email, firstName, lastName);
        var student = new Student(studentCode, user.Id, career, semester);
        SetProperty(student, "User", user);
        return student;
    }

    public static Subject CreateSubject(
        string code = "IS-MAT1",
        string name = "Cálculo Diferencial",
        string description = "Fundamentos del cálculo diferencial e integral",
        int credits = 4,
        string career = "Ingeniería en Sistemas",
        int semesterLevel = 1,
        bool appliesToAllCareers = false)
        => new(code, name, description, credits, career, semesterLevel, appliesToAllCareers);

    public static SubjectSection CreateSection(
        Guid professorId,
        Guid? subjectId = null,
        string sectionCode = "A",
        DayOfWeekType dayOfWeek = DayOfWeekType.Lunes,
        string modality = "Presencial",
        int capacity = 30,
        Subject? subject = null)
    {
        var sid = subjectId ?? subject?.Id ?? Guid.NewGuid();
        var section = new SubjectSection(
            sid, professorId, sectionCode, dayOfWeek,
            new TimeOnly(8, 0), new TimeOnly(10, 0), modality, capacity);
        if (subject is not null)
            SetProperty(section, "Subject", subject);
        return section;
    }

    public static Grade CreateGrade(
        Guid? studentId = null,
        Guid? subjectId = null,
        decimal value = 8.5m,
        string period = "2025-1",
        Guid? gradedByUserId = null,
        string? comments = null,
        Guid? sectionId = null)
        => new(
            studentId ?? Guid.NewGuid(),
            subjectId ?? Guid.NewGuid(),
            sectionId,
            value, period,
            gradedByUserId ?? Guid.NewGuid(),
            comments);

    public static Grade CreateGradeWithNavigation(
        Student student,
        Subject subject,
        decimal value = 8.5m,
        string period = "2025-1",
        string? comments = null,
        Guid? sectionId = null)
    {
        var grade = CreateGrade(student.Id, subject.Id, value, period, comments: comments, sectionId: sectionId);
        SetProperty(grade, "Student", student);
        SetProperty(grade, "Subject", subject);
        return grade;
    }

    public static SectionEnrollment CreateEnrollment(Guid studentId, Guid sectionId)
        => new(studentId, sectionId);

    public static void AddEnrollmentToSection(SubjectSection section, SectionEnrollment enrollment)
    {
        var collection = (ICollection<SectionEnrollment>)GetProperty<SubjectSection, object>(section, "Enrollments")!;
        collection.Add(enrollment);
    }

    public static void AddSectionToSubject(Subject subject, SubjectSection section)
    {
        var collection = (ICollection<SubjectSection>)GetProperty<Subject, object>(subject, "Sections")!;
        collection.Add(section);
    }

    public static void AddGrades(Student student, params Grade[] grades)
    {
        var collection = (ICollection<Grade>)GetProperty<Student, object>(student, "Grades")!;
        foreach (var g in grades)
            collection.Add(g);
    }

    public static void AddGradeToStudent(Student student, Grade grade) =>
        AddGrades(student, grade);

    public static void SetProperty<TEntity>(TEntity entity, string name, object? value)
        where TEntity : class
    {
        var type = entity.GetType();
        PropertyInfo? prop = null;

        while (type is not null && prop is null)
        {
            prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (prop is null)
                type = type.BaseType;
        }

        if (prop is null)
            throw new InvalidOperationException(
                $"Property '{name}' not found on '{entity.GetType().Name}' or its base types.");

        prop.SetValue(entity, value);
    }

    private static TValue? GetProperty<TEntity, TValue>(TEntity entity, string name)
        where TEntity : class
    {
        var prop = typeof(TEntity).GetProperty(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return (TValue?)prop?.GetValue(entity);
    }
}
