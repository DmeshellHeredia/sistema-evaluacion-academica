using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;
using SistemaEvaluacionAcademica.IntegrationTests.Fixtures;

namespace SistemaEvaluacionAcademica.IntegrationTests.API;

/// <summary>
/// Tests de inscripción contra la base de datos Testcontainers compartida de la colección.
///
/// Diseño de aislamiento intencional: en lugar de crear estudiantes nuevos en cada test,
/// los helpers reutilizan estudiantes sembrados (EST-2025-A001, EST-2025-A002) con
/// un paso de limpieza explícito al inicio de cada test (Deactivate de inscripciones
/// previas para el mismo estudiante/materia). Esto es más rápido que crear usuarios
/// con todos sus roles/relaciones y es seguro porque los tests corren secuencialmente
/// dentro de la colección "Integration" (ICollectionFixture, no paralelos).
///
/// Tests que requieren múltiples estudiantes simultáneos (concurrencia) crean sus
/// propios estudiantes frescos vía <see cref="CreateConcurrentEnrollmentScenarioAsync"/>.
/// </summary>
[Trait("Category", "Integration")]
public class EnrollmentEndpointsTests : IntegrationTestBase
{
    public EnrollmentEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── POST /api/enrollments ────────────────────────────────────────────────

    [Fact]
    public async Task Enroll_ValidRequest_Returns200()
    {
        var (studentId, sectionId) = await CreateFreshSectionAsync(capacity: 30, code: "EN-TST-A");
        var client = CreateAuthenticatedClient("Admin");

        var response = await client.PostAsJsonAsync("/api/enrollments", new { studentId, sectionId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Enroll_AlreadyEnrolled_Returns400()
    {
        var (studentId, sectionId) = await CreateFreshSectionAsync(capacity: 30, code: "EN-TST-B");

        await ExecuteInScopeAsync(async db =>
        {
            await db.SectionEnrollments.AddAsync(new SectionEnrollment(studentId, sectionId));
            await db.SaveChangesAsync();
            return true;
        });

        var client = CreateAuthenticatedClient("Admin");
        var response = await client.PostAsJsonAsync("/api/enrollments", new { studentId, sectionId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Enroll_SectionFull_Returns400()
    {
        var (studentId, sectionId) = await CreateFreshSectionAsync(capacity: 1, code: "EN-TST-C");

        // Llenar la sección con la inscripción de otro estudiante
        var fillerId = await ExecuteInScopeAsync(async db =>
            (await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-A001")).Id);

        await ExecuteInScopeAsync(async db =>
        {
            await db.SectionEnrollments.AddAsync(new SectionEnrollment(fillerId, sectionId));
            await db.SaveChangesAsync();
            return true;
        });

        var client = CreateAuthenticatedClient("Admin");
        var response = await client.PostAsJsonAsync("/api/enrollments", new { studentId, sectionId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Enroll_ConcurrentDuplicateRequests_OnlyOneSucceeds()
    {
        var (studentId, sectionId) = await CreateFreshSectionAsync(capacity: 30, code: "EN-TST-D");
        var body = new { studentId, sectionId };

        var t1 = CreateAuthenticatedClient("Admin").PostAsJsonAsync("/api/enrollments", body);
        var t2 = CreateAuthenticatedClient("Admin").PostAsJsonAsync("/api/enrollments", body);
        var results = await Task.WhenAll(t1, t2);

        var codes = results.Select(r => (int)r.StatusCode).ToList();

        codes.Should().Contain(c => c == 200, "at least one request must succeed");
        codes.Should().Contain(c => c >= 400, "at least one request must be rejected as duplicate");
    }

    // ── Prerequisite enforcement ─────────────────────────────────────────────

    /// <summary>
    /// Fresh IS-career semester-2 student with NO grade for IS-MAT1
    /// cannot enroll in IS-MAT2 (which requires IS-MAT1).
    /// </summary>
    [Fact]
    public async Task Enroll_PrerequisiteNotMet_Returns400WithPrereqCode()
    {
        var (studentId, sectionId) = await CreateStudentForPrereqTestAsync(
            sectionCode: "PR-MAT2-A", hasPassedMat1: false);
        var client = CreateAuthenticatedClient("Admin");

        var response = await client.PostAsJsonAsync("/api/enrollments", new { studentId, sectionId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("IS-MAT1");
    }

    /// <summary>
    /// Fresh IS-career semester-2 student WITH a passing grade for IS-MAT1
    /// can enroll in IS-MAT2.
    /// </summary>
    [Fact]
    public async Task Enroll_PrerequisiteMet_Returns200()
    {
        var (studentId, sectionId) = await CreateStudentForPrereqTestAsync(
            sectionCode: "PR-MAT2-B", hasPassedMat1: true);
        var client = CreateAuthenticatedClient("Admin");

        var response = await client.PostAsJsonAsync("/api/enrollments", new { studentId, sectionId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Fresh IS-career semester-2 student WITH a failing grade (5.0) for IS-MAT1
    /// cannot enroll in IS-MAT2 even though a grade record exists — it must be >= 7.
    /// </summary>
    [Fact]
    public async Task Enroll_PrerequisiteReprobado_Returns400WithPrereqCode()
    {
        var (studentId, sectionId) = await CreateStudentForPrereqTestWithFailingGradeAsync("PR-MAT2-C");
        var client = CreateAuthenticatedClient("Admin");

        var response = await client.PostAsJsonAsync("/api/enrollments", new { studentId, sectionId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("IS-MAT1");
        body.Should().Contain("7");
    }

    // ── Schedule conflict & soft-delete ──────────────────────────────────────

    /// <summary>
    /// Carlos Ruiz (EST-2025-A002) is seeded enrolled in IS-MAT1 on Lunes 08:00-10:00.
    /// Creating a new IS-ALG1 section at the same day/time should produce a 400.
    /// </summary>
    [Fact]
    public async Task Enroll_ScheduleConflict_Returns400WithConflictMessage()
    {
        var (studentId, sectionId) = await CreateConflictSectionAsync("SC-ALG1-A");
        var client = CreateAuthenticatedClient("Admin");

        var response = await client.PostAsJsonAsync("/api/enrollments", new { studentId, sectionId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().ContainAny("Choque", "horario", "conflicta");
    }

    /// <summary>
    /// Enrolling in a soft-deleted section should return 404 because the global
    /// filter hides it and GetSectionByIdAsync returns null.
    /// </summary>
    [Fact]
    public async Task Enroll_SoftDeletedSection_Returns404()
    {
        var (studentId, sectionId) = await CreateFreshSectionAsync(capacity: 30, code: "EN-TST-DEL");

        await ExecuteInScopeAsync(async db =>
        {
            var section = await db.SubjectSections.FindAsync(sectionId);
            section!.Deactivate();
            await db.SaveChangesAsync();
            return true;
        });

        var client = CreateAuthenticatedClient("Admin");
        var response = await client.PostAsJsonAsync("/api/enrollments", new { studentId, sectionId });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Concurrent enrollment ────────────────────────────────────────────────

    /// <summary>
    /// N distinct valid students compete for the last spot in a capacity-1 section.
    /// The SERIALIZABLE transaction + capacity re-check in EnrollAsync must ensure
    /// exactly one succeeds and every other request gets a controlled 4xx (not 500).
    /// </summary>
    [Fact]
    public async Task Enroll_ConcurrentCapacityOne_ExactlyOneSucceeds()
    {
        const int studentCount = 8;
        var (studentIds, sectionId) = await CreateConcurrentEnrollmentScenarioAsync(
            capacity: 1, studentCount: studentCount, code: "CC-CAP1-A");

        var tasks = studentIds
            .Select(sid => CreateAuthenticatedClient("Admin")
                .PostAsJsonAsync("/api/enrollments", new { studentId = sid, sectionId }))
            .ToList();

        var responses = await Task.WhenAll(tasks);
        var statusCodes = responses.Select(r => (int)r.StatusCode).ToList();

        statusCodes.Should().NotContain(500, "no internal server errors should occur under contention");
        statusCodes.Count(c => c == 200).Should().Be(1, "exactly one enrollment should succeed");
        statusCodes.Where(c => c != 200).Should().OnlyContain(
            c => c >= 400 && c < 500, "all failures should be controlled 4xx responses");

        var activeCount = await ExecuteInScopeAsync(async db =>
            await db.SectionEnrollments
                .CountAsync(e => e.SectionId == sectionId && e.IsActive));

        activeCount.Should().Be(1, "exactly one active enrollment must exist in the database — no over-enrollment");
    }

    /// <summary>
    /// One student with 21 credits already enrolled concurrently tries to enroll
    /// in two different 3-credit sections. Pre-checks both pass (21+3=24 ≤ 24),
    /// but the in-tx GetEnrolledSectionsAsync re-check ensures the second commit
    /// sees 24 credits and rejects (24+3=27 > 24).
    /// </summary>
    [Fact]
    public async Task Enroll_ConcurrentCreditLimit_NeverExceedsMaxCredits()
    {
        var (studentId, sectionIdA, sectionIdB) = await CreateCreditLimitRaceScenarioAsync("CLR-TST-A", "CLR-TST-B");

        var t1 = CreateAuthenticatedClient("Admin")
            .PostAsJsonAsync("/api/enrollments", new { studentId, sectionId = sectionIdA });
        var t2 = CreateAuthenticatedClient("Admin")
            .PostAsJsonAsync("/api/enrollments", new { studentId, sectionId = sectionIdB });

        var results = await Task.WhenAll(t1, t2);
        var codes   = results.Select(r => (int)r.StatusCode).ToList();

        codes.Should().NotContain(500, "no internal error must occur under concurrent credit pressure");
        codes.Count(c => c == 200).Should().Be(1, "exactly one enrollment must succeed at the credit limit");
        codes.Where(c => c != 200).Should().OnlyContain(c => c >= 400 && c < 500);

        var totalCredits = await ExecuteInScopeAsync(async db =>
        {
            var sectionIds = await db.SectionEnrollments
                .Where(e => e.StudentId == studentId && e.IsActive)
                .Select(e => e.SectionId)
                .ToListAsync();

            return await db.SubjectSections
                .Where(s => sectionIds.Contains(s.Id))
                .SumAsync(s => s.Subject!.Credits);
        });

        totalCredits.Should().BeLessThanOrEqualTo(24, "total enrolled credits must never exceed the limit");
    }

    /// <summary>
    /// One student with no prior enrollments concurrently enrolls in two sections
    /// that overlap in time. The in-tx GetEnrolledSectionsAsync re-check detects the
    /// schedule conflict once the first tx commits and rejects the second.
    /// </summary>
    [Fact]
    public async Task Enroll_ConcurrentScheduleConflict_OnlyOneSucceeds()
    {
        var (studentId, sectionIdA, sectionIdB) = await CreateScheduleConflictRaceScenarioAsync("SCR-TST-A", "SCR-TST-B");

        var t1 = CreateAuthenticatedClient("Admin")
            .PostAsJsonAsync("/api/enrollments", new { studentId, sectionId = sectionIdA });
        var t2 = CreateAuthenticatedClient("Admin")
            .PostAsJsonAsync("/api/enrollments", new { studentId, sectionId = sectionIdB });

        var results = await Task.WhenAll(t1, t2);
        var codes   = results.Select(r => (int)r.StatusCode).ToList();

        codes.Should().NotContain(500, "no internal error must occur under concurrent schedule pressure");
        codes.Count(c => c == 200).Should().Be(1, "exactly one overlapping section must succeed");
        codes.Where(c => c != 200).Should().OnlyContain(c => c >= 400 && c < 500);

        var activeCount = await ExecuteInScopeAsync(db =>
            db.SectionEnrollments
                .CountAsync(e => e.StudentId == studentId
                             && (e.SectionId == sectionIdA || e.SectionId == sectionIdB)
                             && e.IsActive));

        activeCount.Should().Be(1, "exactly one conflicting section must be enrolled in the database");
    }

    // ── Enrollment period gate ───────────────────────────────────────────────

    /// <summary>
    /// Cierra el período activo → POST /api/enrollments devuelve 400 con mensaje de período cerrado.
    /// El período se reabre en el bloque finally para no afectar tests posteriores.
    /// </summary>
    [Fact]
    public async Task Enroll_WithClosedPeriod_Returns400WithEnrollmentMessage()
    {
        var (studentId, sectionId) = await CreateFreshSectionAsync(capacity: 30, code: "EN-CLOSED-A");

        await ExecuteInScopeAsync(async db =>
        {
            var periods = await db.AcademicPeriods.Where(p => p.IsEnrollmentOpen).ToListAsync();
            foreach (var p in periods) p.CloseEnrollment();
            await db.SaveChangesAsync();
            return true;
        });

        try
        {
            var client = CreateAuthenticatedClient("Admin");
            var response = await client.PostAsJsonAsync("/api/enrollments", new { studentId, sectionId });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("selección de materias");
        }
        finally
        {
            await ExecuteInScopeAsync(async db =>
            {
                var period = await db.AcademicPeriods.FirstAsync(p => p.Code == "2025-1");
                period.OpenEnrollment();
                await db.SaveChangesAsync();
                return true;
            });
        }
    }

    /// <summary>
    /// Cierra el período después de inscribir → DELETE /api/enrollments devuelve 400.
    /// El período se reabre en el bloque finally para no afectar tests posteriores.
    /// </summary>
    [Fact]
    public async Task Unenroll_WithClosedPeriod_Returns400()
    {
        var (studentId, sectionId) = await CreateFreshSectionAsync(capacity: 30, code: "EN-CLOSED-B");
        var client = CreateAuthenticatedClient("Admin");

        var enrollResponse = await client.PostAsJsonAsync("/api/enrollments", new { studentId, sectionId });
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await ExecuteInScopeAsync(async db =>
        {
            var periods = await db.AcademicPeriods.Where(p => p.IsEnrollmentOpen).ToListAsync();
            foreach (var p in periods) p.CloseEnrollment();
            await db.SaveChangesAsync();
            return true;
        });

        try
        {
            var response = await client.DeleteAsync($"/api/enrollments/{studentId}/{sectionId}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("selección de materias");
        }
        finally
        {
            await ExecuteInScopeAsync(async db =>
            {
                var period = await db.AcademicPeriods.FirstAsync(p => p.Code == "2025-1");
                period.OpenEnrollment();
                await db.SaveChangesAsync();
                return true;
            });
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // Crea una sección nueva para IS-ALG1 (sin prereqs, semestre 1, carrera IS).
    // Usa EST-2025-A002 (Carlos Ruiz, IS sem 1) como estudiante objetivo.
    // Limpia inscripciones previas a IS-ALG1 de ese estudiante para que los tests
    // no se interfieran entre sí en la base de datos de tests compartida.
    private Task<(Guid studentId, Guid sectionId)> CreateFreshSectionAsync(int capacity, string code) =>
        ExecuteInScopeAsync(async db =>
        {
            var student   = await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-A002");
            var subject   = await db.Subjects.FirstAsync(s => s.Code == "IS-ALG1");
            var professor = await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com");

            // Desactivar inscripciones previas a IS-ALG1 del estudiante para que tests
            // anteriores exitosos no bloqueen ejecuciones posteriores.
            var stale = await db.SectionEnrollments
                .Where(e => e.StudentId == student.Id
                         && e.Section.SubjectId == subject.Id
                         && e.IsActive)
                .ToListAsync();
            foreach (var e in stale) e.Deactivate();

            var section = new SubjectSection(
                subject.Id, professor.Id, code,
                DayOfWeekType.Viernes, new TimeOnly(14, 0), new TimeOnly(16, 0),
                "Virtual", capacity);

            await db.SubjectSections.AddAsync(section);
            await db.SaveChangesAsync();

            return (student.Id, section.Id);
        });

    // Crea un estudiante nuevo de carrera IS semestre 2 (sin inscripciones previas).
    // Opcionalmente siembra una calificación aprobatoria para IS-MAT1 de modo que el prereq de IS-MAT2 se cumpla.
    // Crea una sección IS-MAT2 dedicada con código único para evitar interferencia entre tests.
    private Task<(Guid studentId, Guid sectionId)> CreateStudentForPrereqTestAsync(
        string sectionCode, bool hasPassedMat1) =>
        ExecuteInScopeAsync(async db =>
        {
            var roleId = (await db.Roles.FirstAsync(r => r.Name == "Estudiante")).Id;
            var user = new User(
                $"prereq.{Guid.NewGuid():N}@test.com",
                "placeholder-not-used-in-test",
                "Prereq", "Tester",
                roleId);
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();

            var student = new Student(
                $"TST-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                user.Id,
                CareerTypes.IngenieriaEnSistemas,
                semester: 2);
            await db.Students.AddAsync(student);
            await db.SaveChangesAsync();

            if (hasPassedMat1)
            {
                var mat1 = await db.Subjects.FirstAsync(s => s.Code == "IS-MAT1");
                var mat1Section = await db.SubjectSections
                    .FirstAsync(s => s.SubjectId == mat1.Id && s.IsActive);
                var grader = await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com");
                var grade = new Grade(student.Id, mat1.Id, mat1Section.Id, 8.5m, "2024-1", grader.Id, "prereq test");
                await db.Grades.AddAsync(grade);
                await db.SaveChangesAsync();
            }

            var mat2 = await db.Subjects.FirstAsync(s => s.Code == "IS-MAT2");
            var professor = await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com");
            var section = new SubjectSection(
                mat2.Id, professor.Id, sectionCode,
                DayOfWeekType.Sabado, new TimeOnly(8, 0), new TimeOnly(10, 0),
                "Virtual", 30);
            await db.SubjectSections.AddAsync(section);
            await db.SaveChangesAsync();

            return (student.Id, section.Id);
        });

    // Crea un estudiante IS semestre 2 con nota reprobatoria (5.0) para IS-MAT1.
    // IS-MAT2 requiere IS-MAT1 aprobado (>= 7), así que la inscripción debe fallar.
    private Task<(Guid studentId, Guid sectionId)> CreateStudentForPrereqTestWithFailingGradeAsync(
        string sectionCode) =>
        ExecuteInScopeAsync(async db =>
        {
            var roleId = (await db.Roles.FirstAsync(r => r.Name == "Estudiante")).Id;
            var user = new User(
                $"prereq.fail.{Guid.NewGuid():N}@test.com",
                "placeholder-not-used-in-test",
                "Prereq", "FailTester",
                roleId);
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();

            var student = new Student(
                $"TST-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                user.Id,
                CareerTypes.IngenieriaEnSistemas,
                semester: 2);
            await db.Students.AddAsync(student);
            await db.SaveChangesAsync();

            // Failing grade (5.0 < 7) for IS-MAT1
            var mat1 = await db.Subjects.FirstAsync(s => s.Code == "IS-MAT1");
            var mat1Section = await db.SubjectSections
                .FirstAsync(s => s.SubjectId == mat1.Id && s.IsActive);
            var grader = await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com");
            var grade = new Grade(student.Id, mat1.Id, mat1Section.Id, 5.0m, "2024-1", grader.Id, "prereq fail test");
            await db.Grades.AddAsync(grade);
            await db.SaveChangesAsync();

            var mat2 = await db.Subjects.FirstAsync(s => s.Code == "IS-MAT2");
            var professor = await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com");
            var section = new SubjectSection(
                mat2.Id, professor.Id, sectionCode,
                DayOfWeekType.Sabado, new TimeOnly(10, 0), new TimeOnly(12, 0),
                "Virtual", 30);
            await db.SubjectSections.AddAsync(section);
            await db.SaveChangesAsync();

            return (student.Id, section.Id);
        });

    // Crea una sección con capacidad N para IS-ALG1 (sin prereqs) y `studentCount` estudiantes
    // nuevos de IS sem-1, todos elegibles para inscribirse. Usada para tests de concurrencia.
    private Task<(Guid[] studentIds, Guid sectionId)> CreateConcurrentEnrollmentScenarioAsync(
        int capacity, int studentCount, string code) =>
        ExecuteInScopeAsync(async db =>
        {
            var subject   = await db.Subjects.FirstAsync(s => s.Code == "IS-ALG1");
            var professor = await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com");
            var roleId    = (await db.Roles.FirstAsync(r => r.Name == "Estudiante")).Id;

            var section = new SubjectSection(
                subject.Id, professor.Id, code,
                DayOfWeekType.Domingo, new TimeOnly(10, 0), new TimeOnly(12, 0),
                "Virtual", capacity);
            await db.SubjectSections.AddAsync(section);
            await db.SaveChangesAsync();

            var studentIds = new Guid[studentCount];
            for (int i = 0; i < studentCount; i++)
            {
                var uid = Guid.NewGuid().ToString("N")[..10].ToUpper();
                var user = new User(
                    $"zcc.{uid.ToLower()}@test.com",
                    "placeholder-not-used-in-test",
                    "Concurrent", $"Student{i + 1}",
                    roleId);
                await db.Users.AddAsync(user);
                await db.SaveChangesAsync();

                // El prefijo "ZCC-" garantiza el orden alfabético después de los códigos sembrados "EST-..."
                // para que GetFirstSeededStudentIdAsync() devuelva siempre el estudiante sembrado correcto.
                var student = new Student(
                    $"ZCC-{uid}",
                    user.Id,
                    CareerTypes.IngenieriaEnSistemas,
                    semester: 1);
                await db.Students.AddAsync(student);
                await db.SaveChangesAsync();

                studentIds[i] = student.Id;
            }

            return (studentIds, section.Id);
        });

    // Carlos Ruiz (EST-2025-A002, IS sem 1) está sembrado inscrito en IS-MAT1 (Lunes 08:00-10:00).
    // Crea una nueva sección IS-ALG1 en el mismo horario para que la inscripción produzca un choque.
    // Desactiva inscripciones previas a IS-ALG1 para que IsEnrolledInAnyOtherSection no se dispare primero.
    private Task<(Guid studentId, Guid sectionId)> CreateConflictSectionAsync(string code) =>
        ExecuteInScopeAsync(async db =>
        {
            var student   = await db.Students.FirstAsync(s => s.StudentCode == "EST-2025-A002");
            var subject   = await db.Subjects.FirstAsync(s => s.Code == "IS-ALG1");
            var professor = await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com");

            var stale = await db.SectionEnrollments
                .Where(e => e.StudentId == student.Id
                         && e.Section.SubjectId == subject.Id
                         && e.IsActive)
                .ToListAsync();
            foreach (var e in stale) e.Deactivate();

            var section = new SubjectSection(
                subject.Id, professor.Id, code,
                DayOfWeekType.Lunes, new TimeOnly(8, 0), new TimeOnly(10, 0),
                "Virtual", 30);
            await db.SubjectSections.AddAsync(section);
            await db.SaveChangesAsync();

            return (student.Id, section.Id);
        });

    // Creates a fresh IS-sem-3 student pre-enrolled in 21 credits (IS-MAT1+IS-PRG1+IS-ALG1+IS-MAT2+IS-PRG2),
    // plus two fresh 3-credit test subjects and their sections.  Used for the concurrent credit-limit race test.
    private Task<(Guid studentId, Guid sectionIdA, Guid sectionIdB)> CreateCreditLimitRaceScenarioAsync(
        string codeA, string codeB) =>
        ExecuteInScopeAsync(async db =>
        {
            var roleId    = (await db.Roles.FirstAsync(r => r.Name == "Estudiante")).Id;
            var professor = await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com");

            var uid  = Guid.NewGuid().ToString("N")[..10].ToUpper();
            var user = new User($"clr.{uid.ToLower()}@test.com", "placeholder", "CreditRace", "Student", roleId);
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();

            var student = new Student($"CLR-{uid}", user.Id, CareerTypes.IngenieriaEnSistemas, semester: 3);
            await db.Students.AddAsync(student);
            await db.SaveChangesAsync();

            // Two test-only 3-credit subjects (no prerequisites) for the concurrent enrollment targets
            var subjectA = new Subject($"TST-{codeA}", $"CreditRace Subject A ({codeA})", "Integration test only", 3, CareerTypes.IngenieriaEnSistemas, 1);
            var subjectB = new Subject($"TST-{codeB}", $"CreditRace Subject B ({codeB})", "Integration test only", 3, CareerTypes.IngenieriaEnSistemas, 1);
            await db.Subjects.AddRangeAsync(subjectA, subjectB);
            await db.SaveChangesAsync();

            // Sections on non-conflicting days and times (far from any seeded section schedule)
            var sectionA = new SubjectSection(subjectA.Id, professor.Id, codeA, DayOfWeekType.Sabado, new TimeOnly(4, 0), new TimeOnly(6, 0), "Virtual", 30);
            var sectionB = new SubjectSection(subjectB.Id, professor.Id, codeB, DayOfWeekType.Domingo, new TimeOnly(4, 0), new TimeOnly(6, 0), "Virtual", 30);
            await db.SubjectSections.AddRangeAsync(sectionA, sectionB);
            await db.SaveChangesAsync();

            // Pre-enroll the student in 21 credits using seeded "A" sections:
            // IS-MAT1 (4) + IS-PRG1 (5) + IS-ALG1 (3) + IS-MAT2 (4) + IS-PRG2 (5) = 21
            var targetCodes = new[] { "IS-MAT1", "IS-PRG1", "IS-ALG1", "IS-MAT2", "IS-PRG2" };
            var subjectIds = await db.Subjects
                .Where(s => targetCodes.Contains(s.Code))
                .Select(s => s.Id)
                .ToListAsync();
            var existingSectionIds = await db.SubjectSections
                .Where(s => subjectIds.Contains(s.SubjectId) && s.SectionCode == "A")
                .Select(s => s.Id)
                .ToListAsync();

            foreach (var sid in existingSectionIds)
                await db.SectionEnrollments.AddAsync(new SectionEnrollment(student.Id, sid));
            await db.SaveChangesAsync();

            return (student.Id, sectionA.Id, sectionB.Id);
        });

    // Creates a fresh IS-sem-1 student with no existing enrollments and two sections
    // (IS-MAT1 and IS-ALG1) that fully overlap in time on the same day.
    // Used for the concurrent schedule-conflict race test.
    private Task<(Guid studentId, Guid sectionIdA, Guid sectionIdB)> CreateScheduleConflictRaceScenarioAsync(
        string codeA, string codeB) =>
        ExecuteInScopeAsync(async db =>
        {
            var roleId    = (await db.Roles.FirstAsync(r => r.Name == "Estudiante")).Id;
            var professor = await db.Users.FirstAsync(u => u.Email == "prof.garcia@academia.com");

            var uid  = Guid.NewGuid().ToString("N")[..10].ToUpper();
            var user = new User($"scr.{uid.ToLower()}@test.com", "placeholder", "ScheduleRace", "Student", roleId);
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();

            var student = new Student($"SCR-{uid}", user.Id, CareerTypes.IngenieriaEnSistemas, semester: 1);
            await db.Students.AddAsync(student);
            await db.SaveChangesAsync();

            // IS-MAT1 and IS-ALG1: no prerequisites, IS semester 1
            var mat1Id = (await db.Subjects.FirstAsync(s => s.Code == "IS-MAT1")).Id;
            var alg1Id = (await db.Subjects.FirstAsync(s => s.Code == "IS-ALG1")).Id;

            // Both sections overlap: Saturday 05:00-07:00 (hour unused by seeded data)
            var sectionA = new SubjectSection(mat1Id, professor.Id, codeA, DayOfWeekType.Sabado, new TimeOnly(5, 0), new TimeOnly(7, 0), "Virtual", 30);
            var sectionB = new SubjectSection(alg1Id, professor.Id, codeB, DayOfWeekType.Sabado, new TimeOnly(5, 0), new TimeOnly(7, 0), "Virtual", 30);
            await db.SubjectSections.AddRangeAsync(sectionA, sectionB);
            await db.SaveChangesAsync();

            return (student.Id, sectionA.Id, sectionB.Id);
        });
}
