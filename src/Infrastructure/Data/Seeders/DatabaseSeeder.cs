using Microsoft.EntityFrameworkCore;
using SistemaEvaluacionAcademica.Domain.Entities;
using SistemaEvaluacionAcademica.Domain.Enums;

namespace SistemaEvaluacionAcademica.Infrastructure.Data.Seeders;

public static class DatabaseSeeder
{
    private static readonly Guid AdminRoleId      = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ProfesorRoleId   = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid EstudianteRoleId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    public static async Task SeedAsync(AppDbContext context)
    {
        // Verifica el usuario admin demo específicamente — más robusto que AnyAsync()
        // porque no salta si la BD tiene otros usuarios pero no los datos demo.
        if (await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == "admin@academia.com"))
            return;

        // CONTRASEÑAS
        var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin123!",      workFactor: 12);
        var profHash  = BCrypt.Net.BCrypt.HashPassword("Profesor123!",   workFactor: 12);
        var estHash   = BCrypt.Net.BCrypt.HashPassword("Estudiante123!", workFactor: 12);

        // USUARIOS
        var admin  = new User("admin@academia.com",          adminHash, "Carlos",   "Administrador", AdminRoleId);
        var prof1  = new User("prof.garcia@academia.com",    profHash,  "Ana",      "García",        ProfesorRoleId);
        var prof2  = new User("prof.lopez@academia.com",     profHash,  "Roberto",  "López",         ProfesorRoleId);
        var prof3  = new User("prof.martinez@academia.com",  profHash,  "Sofía",    "Martínez",      ProfesorRoleId);

        var uEst1  = new User("juan.perez@academia.com",     estHash,   "Juan",     "Pérez",         EstudianteRoleId);
        var uEst2  = new User("carlos.ruiz@academia.com",    estHash,   "Carlos",   "Ruiz",          EstudianteRoleId);
        var uEst3  = new User("lucia.torres@academia.com",   estHash,   "Lucía",    "Torres",        EstudianteRoleId);
        var uEst4  = new User("sofia.mendez@academia.com",   estHash,   "Sofía",    "Méndez",        EstudianteRoleId);

        context.Users.AddRange(admin, prof1, prof2, prof3, uEst1, uEst2, uEst3, uEst4);
        await context.SaveChangesAsync();

        // ESTUDIANTES
        var estIS1  = new Student("EST-2025-A001", uEst1.Id, CareerTypes.IngenieriaEnSistemas,  3);
        var estIS2  = new Student("EST-2025-A002", uEst2.Id, CareerTypes.IngenieriaEnSistemas,  1);
        var estCIB1 = new Student("EST-2025-B001", uEst3.Id, CareerTypes.Ciberseguridad,        2);
        var estDS1  = new Student("EST-2025-C001", uEst4.Id, CareerTypes.DesarrolloDeSoftware,  3);

        context.Students.AddRange(estIS1, estIS2, estCIB1, estDS1);
        await context.SaveChangesAsync();

        // MATERIAS — INGENIERÍA EN SISTEMAS
        var is_mat1  = new Subject("IS-MAT1",  "Matemáticas I",          "Cálculo diferencial",                    4, CareerTypes.IngenieriaEnSistemas, 1);
        var is_prog1 = new Subject("IS-PRG1",  "Programación I",         "Fundamentos de programación en C",       5, CareerTypes.IngenieriaEnSistemas, 1);
        var is_alg1  = new Subject("IS-ALG1",  "Álgebra Lineal",         "Vectores, matrices y sistemas lineales", 3, CareerTypes.IngenieriaEnSistemas, 1);
        var is_mat2  = new Subject("IS-MAT2",  "Matemáticas II",         "Cálculo integral y series",              4, CareerTypes.IngenieriaEnSistemas, 2);
        var is_prog2 = new Subject("IS-PRG2",  "Programación II",        "POO y estructuras de datos",             5, CareerTypes.IngenieriaEnSistemas, 2);
        var is_bd1   = new Subject("IS-BD1",   "Bases de Datos I",       "Modelo relacional y SQL",                4, CareerTypes.IngenieriaEnSistemas, 2);
        var is_redes = new Subject("IS-RED1",  "Redes de Computadoras",  "Modelo OSI y protocolos TCP/IP",         4, CareerTypes.IngenieriaEnSistemas, 3);
        var is_so    = new Subject("IS-SO1",   "Sistemas Operativos",    "Gestión de procesos y memoria",          4, CareerTypes.IngenieriaEnSistemas, 3);
        var is_bd2   = new Subject("IS-BD2",   "Bases de Datos II",      "Optimización y NoSQL",                   4, CareerTypes.IngenieriaEnSistemas, 3);

        // MATERIAS — CIBERSEGURIDAD
        var cib_fund = new Subject("CIB-FUN1", "Fundamentos de Ciberseguridad", "Conceptos base y amenazas",       3, CareerTypes.Ciberseguridad, 1);
        var cib_prog = new Subject("CIB-PRG1", "Programación para Seguridad",   "Python y scripting ofensivo/defensivo", 4, CareerTypes.Ciberseguridad, 1);
        var cib_red1 = new Subject("CIB-RED1", "Redes I",                       "Protocolos y arquitecturas de red", 3, CareerTypes.Ciberseguridad, 1);
        var cib_cript= new Subject("CIB-CRP1", "Criptografía",                  "Cifrado simétrico y asimétrico",   4, CareerTypes.Ciberseguridad, 2);
        var cib_red2 = new Subject("CIB-RED2", "Redes II",                      "Seguridad en redes y firewalls",   4, CareerTypes.Ciberseguridad, 2);
        var cib_so   = new Subject("CIB-SO1",  "Sistemas Operativos Seguros",   "Hardening Linux y Windows",        4, CareerTypes.Ciberseguridad, 2);
        var cib_hack = new Subject("CIB-HAK1", "Hacking Ético",                 "Pentesting y metodologías OWASP",  5, CareerTypes.Ciberseguridad, 3);
        var cib_for  = new Subject("CIB-FOR1", "Forense Digital",               "Análisis de evidencias digitales", 4, CareerTypes.Ciberseguridad, 3);

        // MATERIAS — DESARROLLO DE SOFTWARE
        var ds_prog1 = new Subject("DS-PRG1",  "Programación I",         "Fundamentos con Python",                 4, CareerTypes.DesarrolloDeSoftware, 1);
        var ds_web1  = new Subject("DS-WEB1",  "Desarrollo Web I",       "HTML, CSS y JavaScript básico",          4, CareerTypes.DesarrolloDeSoftware, 1);
        var ds_bd1   = new Subject("DS-BD1",   "Bases de Datos I",       "SQL y diseño relacional",                3, CareerTypes.DesarrolloDeSoftware, 1);
        var ds_prog2 = new Subject("DS-PRG2",  "Programación II",        "POO y patrones de diseño",               5, CareerTypes.DesarrolloDeSoftware, 2);
        var ds_web2  = new Subject("DS-WEB2",  "Desarrollo Web II",      "React y frameworks modernos",            4, CareerTypes.DesarrolloDeSoftware, 2);
        var ds_api   = new Subject("DS-API1",  "APIs REST",              "Diseño e implementación de APIs",        4, CareerTypes.DesarrolloDeSoftware, 2);
        var ds_cloud = new Subject("DS-CLD1",  "Cloud Computing",        "AWS, Azure y contenerización",           4, CareerTypes.DesarrolloDeSoftware, 3);
        var ds_test  = new Subject("DS-TST1",  "Testing y QA",           "Unit, integration y E2E testing",        3, CareerTypes.DesarrolloDeSoftware, 3);
        var ds_arch  = new Subject("DS-ARC1",  "Arquitectura de Software","Microservicios y Clean Architecture",    4, CareerTypes.DesarrolloDeSoftware, 3);

        context.Subjects.AddRange(
            is_mat1, is_prog1, is_alg1, is_mat2, is_prog2, is_bd1, is_redes, is_so, is_bd2,
            cib_fund, cib_prog, cib_red1, cib_cript, cib_red2, cib_so, cib_hack, cib_for,
            ds_prog1, ds_web1, ds_bd1, ds_prog2, ds_web2, ds_api, ds_cloud, ds_test, ds_arch
        );
        await context.SaveChangesAsync();

        // PREREQUISITOS
        context.SubjectPrerequisites.AddRange(
            new SubjectPrerequisite(is_mat2.Id,  is_mat1.Id),
            new SubjectPrerequisite(is_prog2.Id, is_prog1.Id),
            new SubjectPrerequisite(is_bd1.Id,   is_prog1.Id),
            new SubjectPrerequisite(is_redes.Id, is_mat2.Id),
            new SubjectPrerequisite(is_so.Id,    is_prog2.Id),
            new SubjectPrerequisite(is_bd2.Id,   is_bd1.Id),
            new SubjectPrerequisite(cib_cript.Id, cib_fund.Id),
            new SubjectPrerequisite(cib_red2.Id,  cib_red1.Id),
            new SubjectPrerequisite(cib_so.Id,    cib_prog.Id),
            new SubjectPrerequisite(cib_hack.Id,  cib_cript.Id),
            new SubjectPrerequisite(cib_hack.Id,  cib_red2.Id),
            new SubjectPrerequisite(cib_for.Id,   cib_so.Id),
            new SubjectPrerequisite(ds_prog2.Id, ds_prog1.Id),
            new SubjectPrerequisite(ds_web2.Id,  ds_web1.Id),
            new SubjectPrerequisite(ds_api.Id,   ds_bd1.Id),
            new SubjectPrerequisite(ds_api.Id,   ds_prog1.Id),
            new SubjectPrerequisite(ds_cloud.Id, ds_api.Id),
            new SubjectPrerequisite(ds_test.Id,  ds_prog2.Id),
            new SubjectPrerequisite(ds_arch.Id,  ds_prog2.Id),
            new SubjectPrerequisite(ds_arch.Id,  ds_api.Id)
        );
        await context.SaveChangesAsync();

        // SECCIONES
        // IS — Presencial
        var sec_is_mat1  = new SubjectSection(is_mat1.Id,  prof1.Id, "A", DayOfWeekType.Lunes,     new TimeOnly(8,0),  new TimeOnly(10,0), "Presencial",      30);
        var sec_is_prog1 = new SubjectSection(is_prog1.Id, prof2.Id, "A", DayOfWeekType.Martes,    new TimeOnly(8,0),  new TimeOnly(10,0), "Presencial",      30);
        var sec_is_alg1  = new SubjectSection(is_alg1.Id,  prof1.Id, "A", DayOfWeekType.Miercoles, new TimeOnly(8,0),  new TimeOnly(10,0), "Presencial",      30);
        var sec_is_mat2  = new SubjectSection(is_mat2.Id,  prof1.Id, "A", DayOfWeekType.Lunes,     new TimeOnly(10,0), new TimeOnly(12,0), "Presencial",      30);
        var sec_is_prog2 = new SubjectSection(is_prog2.Id, prof2.Id, "A", DayOfWeekType.Martes,    new TimeOnly(10,0), new TimeOnly(12,0), "Presencial",      30);
        var sec_is_bd1   = new SubjectSection(is_bd1.Id,   prof3.Id, "A", DayOfWeekType.Jueves,    new TimeOnly(8,0),  new TimeOnly(10,0), "Presencial",      30);
        var sec_is_redes = new SubjectSection(is_redes.Id, prof2.Id, "A", DayOfWeekType.Lunes,     new TimeOnly(12,0), new TimeOnly(14,0), "Presencial",      30);
        var sec_is_so    = new SubjectSection(is_so.Id,    prof2.Id, "A", DayOfWeekType.Martes,    new TimeOnly(12,0), new TimeOnly(14,0), "Presencial",      30);
        var sec_is_bd2   = new SubjectSection(is_bd2.Id,   prof3.Id, "A", DayOfWeekType.Jueves,    new TimeOnly(10,0), new TimeOnly(12,0), "Presencial",      30);

        // CIB — Semipresencial
        var sec_cib_fund = new SubjectSection(cib_fund.Id, prof3.Id, "A", DayOfWeekType.Lunes,     new TimeOnly(8,0),  new TimeOnly(10,0), "Semipresencial",  25);
        var sec_cib_prog = new SubjectSection(cib_prog.Id, prof2.Id, "A", DayOfWeekType.Martes,    new TimeOnly(8,0),  new TimeOnly(10,0), "Semipresencial",  25);
        var sec_cib_red1 = new SubjectSection(cib_red1.Id, prof2.Id, "A", DayOfWeekType.Miercoles, new TimeOnly(8,0),  new TimeOnly(10,0), "Semipresencial",  25);
        var sec_cib_crp1 = new SubjectSection(cib_cript.Id,prof3.Id, "A", DayOfWeekType.Lunes,     new TimeOnly(10,0), new TimeOnly(12,0), "Semipresencial",  25);
        var sec_cib_red2 = new SubjectSection(cib_red2.Id, prof2.Id, "A", DayOfWeekType.Martes,    new TimeOnly(10,0), new TimeOnly(12,0), "Semipresencial",  25);
        var sec_cib_so   = new SubjectSection(cib_so.Id,   prof3.Id, "A", DayOfWeekType.Miercoles, new TimeOnly(10,0), new TimeOnly(12,0), "Semipresencial",  25);
        var sec_cib_hak1 = new SubjectSection(cib_hack.Id, prof3.Id, "A", DayOfWeekType.Lunes,     new TimeOnly(12,0), new TimeOnly(14,0), "Semipresencial",  25);
        var sec_cib_for1 = new SubjectSection(cib_for.Id,  prof3.Id, "A", DayOfWeekType.Martes,    new TimeOnly(12,0), new TimeOnly(14,0), "Semipresencial",  25);

        // DS — Virtual
        var sec_ds_prg1  = new SubjectSection(ds_prog1.Id, prof1.Id, "A", DayOfWeekType.Lunes,     new TimeOnly(8,0),  new TimeOnly(10,0), "Virtual",         35);
        var sec_ds_web1  = new SubjectSection(ds_web1.Id,  prof1.Id, "A", DayOfWeekType.Martes,    new TimeOnly(8,0),  new TimeOnly(10,0), "Virtual",         35);
        var sec_ds_bd1   = new SubjectSection(ds_bd1.Id,   prof3.Id, "A", DayOfWeekType.Miercoles, new TimeOnly(8,0),  new TimeOnly(10,0), "Virtual",         35);
        var sec_ds_prg2  = new SubjectSection(ds_prog2.Id, prof1.Id, "A", DayOfWeekType.Lunes,     new TimeOnly(10,0), new TimeOnly(12,0), "Virtual",         35);
        var sec_ds_web2  = new SubjectSection(ds_web2.Id,  prof1.Id, "A", DayOfWeekType.Martes,    new TimeOnly(10,0), new TimeOnly(12,0), "Virtual",         35);
        var sec_ds_api1  = new SubjectSection(ds_api.Id,   prof2.Id, "A", DayOfWeekType.Miercoles, new TimeOnly(10,0), new TimeOnly(12,0), "Virtual",         35);
        var sec_ds_cld1  = new SubjectSection(ds_cloud.Id, prof2.Id, "A", DayOfWeekType.Lunes,     new TimeOnly(12,0), new TimeOnly(14,0), "Virtual",         35);
        var sec_ds_tst1  = new SubjectSection(ds_test.Id,  prof1.Id, "A", DayOfWeekType.Martes,    new TimeOnly(12,0), new TimeOnly(14,0), "Virtual",         35);
        var sec_ds_arc1  = new SubjectSection(ds_arch.Id,  prof2.Id, "A", DayOfWeekType.Miercoles, new TimeOnly(12,0), new TimeOnly(14,0), "Virtual",         35);

        context.SubjectSections.AddRange(
            sec_is_mat1, sec_is_prog1, sec_is_alg1, sec_is_mat2, sec_is_prog2, sec_is_bd1,
            sec_is_redes, sec_is_so, sec_is_bd2,
            sec_cib_fund, sec_cib_prog, sec_cib_red1, sec_cib_crp1, sec_cib_red2, sec_cib_so,
            sec_cib_hak1, sec_cib_for1,
            sec_ds_prg1, sec_ds_web1, sec_ds_bd1, sec_ds_prg2, sec_ds_web2, sec_ds_api1,
            sec_ds_cld1, sec_ds_tst1, sec_ds_arc1
        );
        await context.SaveChangesAsync();

        // MATRÍCULAS EN SECCIONES
        // estIS1 (sem 3): inscritos en sem 1, 2 y 3
        context.SectionEnrollments.AddRange(
            new SectionEnrollment(estIS1.Id, sec_is_mat1.Id),
            new SectionEnrollment(estIS1.Id, sec_is_prog1.Id),
            new SectionEnrollment(estIS1.Id, sec_is_alg1.Id),
            new SectionEnrollment(estIS1.Id, sec_is_mat2.Id),
            new SectionEnrollment(estIS1.Id, sec_is_prog2.Id),
            new SectionEnrollment(estIS1.Id, sec_is_bd1.Id),
            new SectionEnrollment(estIS1.Id, sec_is_redes.Id),
            new SectionEnrollment(estIS1.Id, sec_is_so.Id),
            new SectionEnrollment(estIS1.Id, sec_is_bd2.Id)
        );

        // estIS2 (sem 1): mat1 y prog1
        context.SectionEnrollments.AddRange(
            new SectionEnrollment(estIS2.Id, sec_is_mat1.Id),
            new SectionEnrollment(estIS2.Id, sec_is_prog1.Id)
        );

        // estCIB1 (sem 2): sem 1 completado + sem 2 actual
        context.SectionEnrollments.AddRange(
            new SectionEnrollment(estCIB1.Id, sec_cib_fund.Id),
            new SectionEnrollment(estCIB1.Id, sec_cib_prog.Id),
            new SectionEnrollment(estCIB1.Id, sec_cib_red1.Id),
            new SectionEnrollment(estCIB1.Id, sec_cib_crp1.Id),
            new SectionEnrollment(estCIB1.Id, sec_cib_red2.Id)
        );

        // estDS1 (sem 3): sem 1, 2 y 3
        context.SectionEnrollments.AddRange(
            new SectionEnrollment(estDS1.Id, sec_ds_prg1.Id),
            new SectionEnrollment(estDS1.Id, sec_ds_web1.Id),
            new SectionEnrollment(estDS1.Id, sec_ds_bd1.Id),
            new SectionEnrollment(estDS1.Id, sec_ds_prg2.Id),
            new SectionEnrollment(estDS1.Id, sec_ds_web2.Id),
            new SectionEnrollment(estDS1.Id, sec_ds_api1.Id),
            new SectionEnrollment(estDS1.Id, sec_ds_cld1.Id),
            new SectionEnrollment(estDS1.Id, sec_ds_tst1.Id),
            new SectionEnrollment(estDS1.Id, sec_ds_arc1.Id)
        );

        await context.SaveChangesAsync();

        // CALIFICACIONES
        // estIS1: sem 1 aprobado, sem 2 aprobado
        context.Grades.AddRange(
            new Grade(estIS1.Id, is_mat1.Id,  sec_is_mat1.Id,  9.0m, "2024-1", prof1.Id, "Excelente"),
            new Grade(estIS1.Id, is_prog1.Id, sec_is_prog1.Id, 8.5m, "2024-1", prof2.Id, "Muy bien"),
            new Grade(estIS1.Id, is_alg1.Id,  sec_is_alg1.Id,  7.5m, "2024-1", prof1.Id, "Bien"),
            new Grade(estIS1.Id, is_mat2.Id,  sec_is_mat2.Id,  8.0m, "2024-2", prof1.Id, "Bien"),
            new Grade(estIS1.Id, is_prog2.Id, sec_is_prog2.Id, 9.0m, "2024-2", prof2.Id, "Excelente"),
            new Grade(estIS1.Id, is_bd1.Id,   sec_is_bd1.Id,   8.5m, "2024-2", prof3.Id, "Muy bien")
        );

        // estCIB1: sem 1 aprobado
        context.Grades.AddRange(
            new Grade(estCIB1.Id, cib_fund.Id, sec_cib_fund.Id, 8.0m, "2024-2", prof3.Id, "Buen manejo de conceptos"),
            new Grade(estCIB1.Id, cib_prog.Id, sec_cib_prog.Id, 7.5m, "2024-2", prof2.Id, "Scripting correcto"),
            new Grade(estCIB1.Id, cib_red1.Id, sec_cib_red1.Id, 9.0m, "2024-2", prof2.Id, "Excelente")
        );

        // estDS1: sem 1 y 2 aprobados
        context.Grades.AddRange(
            new Grade(estDS1.Id, ds_prog1.Id, sec_ds_prg1.Id, 8.0m, "2024-1", prof1.Id, "Bien"),
            new Grade(estDS1.Id, ds_web1.Id,  sec_ds_web1.Id, 7.0m, "2024-1", prof1.Id, "Aprobó"),
            new Grade(estDS1.Id, ds_bd1.Id,   sec_ds_bd1.Id,  8.5m, "2024-1", prof3.Id, "Muy bien"),
            new Grade(estDS1.Id, ds_prog2.Id, sec_ds_prg2.Id, 9.0m, "2024-2", prof1.Id, "Excelente"),
            new Grade(estDS1.Id, ds_web2.Id,  sec_ds_web2.Id, 8.0m, "2024-2", prof1.Id, "Bien"),
            new Grade(estDS1.Id, ds_api.Id,   sec_ds_api1.Id, 8.5m, "2024-2", prof2.Id, "Muy bien")
        );

        await context.SaveChangesAsync();

        // PERÍODO ACADÉMICO ACTIVO
        var activePeriod = new AcademicPeriod(
            "Primer Semestre 2025", "2025-1",
            new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 7, 15, 0, 0, 0, DateTimeKind.Utc));
        activePeriod.OpenEnrollment();
        context.AcademicPeriods.Add(activePeriod);
        await context.SaveChangesAsync();

        Console.WriteLine("[DatabaseSeeder] Datos académicos insertados.");
        Console.WriteLine("  Admin:       admin@academia.com              / Admin123!");
        Console.WriteLine("  Profesor:    prof.garcia@academia.com        / Profesor123!");
        Console.WriteLine("  Estudiante IS sem3: juan.perez@academia.com  / Estudiante123!");
        Console.WriteLine("  Estudiante IS sem1: carlos.ruiz@academia.com / Estudiante123!");
        Console.WriteLine("  Estudiante CIB sem2: lucia.torres@academia.com / Estudiante123!");
        Console.WriteLine("  Estudiante DS sem3: sofia.mendez@academia.com  / Estudiante123!");
    }
}
