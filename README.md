# 🎓 Sistema de Evaluación Académica

> Clean Architecture · ASP.NET Core 8 · Next.js 16 · SQL Server

[![CI](https://img.shields.io/github/actions/workflow/status/DmeshellHeredia/sistema-evaluacion-academica/ci.yml?label=CI&style=flat-square)](https://github.com/DmeshellHeredia/sistema-evaluacion-academica/actions)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Next.js](https://img.shields.io/badge/Next.js-16-black?style=flat-square&logo=next.js)](https://nextjs.org/)
[![License](https://img.shields.io/badge/license-MIT-3b82f6?style=flat-square)](./LICENSE)

Sistema académico full-stack que gestiona inscripciones, prerequisitos, calificaciones y actividades LMS para tres carreras universitarias. Los estudiantes navegan un catálogo con estado en tiempo real — disponible · inscrita · bloqueada por prerequisitos · choque de horario — y se inscriben durante períodos abiertos. Los profesores gestionan secciones, crean actividades y califican entregas. Los administradores controlan todo.

---

## ⚡ Lo que destaca técnicamente

| # | Qué | Por qué importa |
|---|-----|-----------------|
| 1 | **Pruebas con Testcontainers** (unit + integración) | SQL Server real en cada run de integración — sin mocks de base de datos |
| 2 | **Transacción Serializable** en inscripción | Exactamente 1 INSERT tiene éxito bajo 8 solicitudes concurrentes simultáneas |
| 3 | **`Result<T>` pattern consistente** | Cero excepciones para flujos esperados — paths de error explícitos y tipados |
| 4 | **Clean Architecture real** | Dependencias unidireccionales enforceadas por el compilador, no solo carpetas |
| 5 | **Detección de ciclos DFS** en prerequisitos | `A → B → C → A` devuelve 400 antes de crear dependencias circulares |

---

## 🛠 Stack

| Capa | Tecnología |
|------|------------|
| **Backend** | ASP.NET Core 8 · EF Core 8 · SQL Server 2022 |
| **Autenticación** | JWT Bearer · BCrypt |
| **Frontend** | Next.js 16 (App Router) · TypeScript · Tailwind CSS v4 · shadcn/ui |
| **Pruebas** | xUnit · Moq · FluentAssertions · Testcontainers |
| **BD local** | Docker Compose (SQL Server 2022 Developer) |
| **CI** | GitHub Actions |

---

## Contenido

- [Arquitectura](#-arquitectura)
- [Flujo de inscripción](#-flujo-de-inscripción)
- [Inicio rápido](#-inicio-rápido)
- [Usuarios demo](#usuarios-demo)
- [Pruebas](#-pruebas)
- [Decisiones técnicas](#decisiones-técnicas)
- [Trade-offs conscientes](#️-trade-offs-conscientes)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Comandos útiles](#comandos-útiles)

---

## 🏗 Arquitectura

```
┌──────────────────────────────────────────────────────────┐
│  API             Controllers · Middleware · Program.cs   │
├──────────────────────────────────────────────────────────┤
│  Application     Services · DTOs · Result<T> · Interfaces│
├──────────────────────────────────────────────────────────┤
│  Infrastructure  EF Core · Repos · JWT · Seeders         │
├──────────────────────────────────────────────────────────┤
│  Domain          Entities · Domain Interfaces  (puro)    │
└──────────────────────────────────────────────────────────┘
        ↑ cada capa solo conoce las capas que están debajo
```

`Domain` no tiene dependencias externas. `Application` solo depende de `Domain`. `Infrastructure` implementa las interfaces que `Application` define. `API` conecta todo vía inyección de dependencias. El compilador enforcea las referencias — no es solo nomenclatura.

Todos los servicios devuelven `Result<T>` en lugar de lanzar excepciones para fallos esperados. `ExceptionHandlingMiddleware` captura lo verdaderamente inesperado y lo mapea a JSON consistente con `X-Request-ID`.

<details>
<summary>Flujo completo de una petición HTTP</summary>

```
Cliente
  │  HTTP request + JWT Bearer token
  ▼
RequestIdMiddleware         → genera / propaga X-Request-ID
  │
ExceptionHandlingMiddleware → captura excepciones → JSON + X-Request-ID
  │
Middleware JWT               → valida token, puebla ClaimsPrincipal
  │
Middleware de autorización  → verifica roles
  │
Controlador
  │  llama
  ▼
Servicio (Application)      → devuelve Result<T>
  │  llama
  ▼
IUnitOfWork → Repositorio → EF Core → SQL Server
  │
Controlador mapea Result<T> → IActionResult
  │
Cliente  ←  HTTP 200 / 400 / 401 / 403 / 404 / 409 / 422 / 500
```

</details>

---

## 🚦 Flujo de inscripción

`POST /api/enrollments` ejecuta 13 validaciones secuenciales antes del commit:

```
 1. ¿Período de selección abierto?                      → 400
 2. ¿Existe el estudiante?                              → 404
 3. ¿Sección existe y está activa?                      → 404
 4. ¿Materia activa (no eliminada lógicamente)?         → 404
 5. ¿Materia aplica a la carrera del estudiante?        → 400
 6. ¿Semestre del estudiante ≥ nivel de la materia?     → 400
 7. ¿Ya inscrito en esta sección?                       → 400
 8. ¿Ya inscrito en otra sección de la misma materia?   → 400
 9. ¿Ya aprobó la materia (nota ≥ 7)?                   → 400
10. ¿Todos los prerequisitos cumplidos (nota ≥ 7)?      → 400 + lista faltante
11. ¿Choque de horario con inscripciones actuales?      → 400 + detalle del conflicto
12. ¿Créditos actuales + nuevos ≤ MaxCreditsPerPeriod?  → 400 + conteo
13. ¿Sección con cupo disponible?                       → 400
    │
    └─ INICIO TRANSACCIÓN SERIALIZABLE
         ├─ Re-verificar inscripción duplicada           → 400 + rollback
         ├─ Re-verificar otra sección misma materia      → 400 + rollback
         ├─ Re-verificar capacidad                       → 400 + rollback
         ├─ Re-verificar créditos (suma fresca)          → 400 + rollback
         ├─ Re-verificar choque de horario               → 400 + rollback
         └─ INSERT SectionEnrollment → COMMIT → 200
```

Las peticiones concurrentes (mismo estudiante + sección) se resuelven con la combinación de aislamiento **Serializable** + índice único `(StudentId, SectionId)` — exactamente un INSERT tiene éxito; el resto reciben 400/409 estructurado.

---

## 🚀 Inicio rápido

### Prerrequisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- EF Core CLI: `dotnet tool install --global dotnet-ef`

### 1. Clonar

```bash
git clone https://github.com/DmeshellHeredia/sistema-evaluacion-academica.git
cd sistema-evaluacion-academica
```

### 2. Variables de entorno

```bash
cp .env.example .env
```

El valor por defecto `SA_PASSWORD=Dev@Password_2024!` funciona sin cambios. Mantenlo sincronizado con `src/API/appsettings.Development.json` si lo cambias.

### 3. Base de datos

```bash
docker compose up -d
docker compose ps   # esperar status "healthy" (~20-30 s)
```

SQL Server 2022 (Developer Edition) en puerto **1444** del host. Los datos persisten en el volumen `sqlserver_data`.

### 4. Backend

```bash
cd src/API
dotnet run
```

- API + Swagger: `http://localhost:5000`
- Health check: `GET http://localhost:5000/health`

En el primer arranque aplica migraciones y siembra usuarios, materias, secciones y calificaciones demo automáticamente. Si SQL Server no está listo, reintenta 5 veces con mensaje claro.

### 5. Frontend

```bash
echo "NEXT_PUBLIC_API_URL=http://localhost:5000" > sistema-evaluacion-academica/.env.local
cd sistema-evaluacion-academica
npm install && npm run dev
```

Abre [http://localhost:3000](http://localhost:3000).

---

## Usuarios demo

| Rol | Correo | Contraseña | Notas |
|-----|--------|------------|-------|
| Admin | `admin@academia.com` | `Admin123!` | Acceso total |
| Profesor | `prof.garcia@academia.com` | `Profesor123!` | Ve sus propias secciones |
| Estudiante — sem 3, IS | `juan.perez@academia.com` | `Estudiante123!` | Tiene calificaciones, materias sem 3 |
| Estudiante — sem 1, IS | `carlos.ruiz@academia.com` | `Estudiante123!` | Puede inscribirse |
| Estudiante — sem 2, CIB | `lucia.torres@academia.com` | `Estudiante123!` | Carrera Ciberseguridad |
| Estudiante — sem 3, DS | `sofia.mendez@academia.com` | `Estudiante123!` | Carrera Desarrollo de Software |

---

## 🧪 Pruebas

```
dotnet test --configuration Release
```

### Unitarias — sin Docker

```bash
dotnet test tests/UnitTests --configuration Release
```

Cubren: servicios, reglas de dominio, prerequisitos, capacidad, créditos, control de acceso, auditoría.

### Integración — requiere Docker Desktop en ejecución

```bash
dotnet test tests/IntegrationTests --configuration Release
```

Testcontainers levanta un **SQL Server desechable** por ejecución: aplica migraciones + seeder, ejecuta las pruebas y lo destruye. Sin estado compartido entre runs, sin mock de base de datos, driver real.

Cubren: flujos HTTP completos, inscripción concurrente, prerequisitos, bloqueo de capacidad, rate limiting, CORS, control de acceso.

### Todas las pruebas .NET

```bash
dotnet test --configuration Release
```

### Coverage — backend

```bash
# Generar XMLs de coverage (UnitTests + IntegrationTests)
dotnet test --configuration Release \
  --collect:"XPlat Code Coverage" \
  --settings coverage.runsettings \
  --results-directory coverage-results

# Instalar ReportGenerator (una sola vez)
dotnet tool install --global dotnet-reportgenerator-globaltool

# Generar reporte HTML + resumen
reportgenerator \
  -reports:"coverage-results/**/coverage.cobertura.xml" \
  -targetdir:"coverage-results/report" \
  -reporttypes:"Html;TextSummary"

# Abrir en el navegador
open coverage-results/report/index.html        # macOS
xdg-open coverage-results/report/index.html   # Linux
start coverage-results/report/index.html       # Windows
```

En CI, el job `backend` genera el reporte automáticamente y lo sube como artifact `coverage-report` (retención: 7 días). Descargable desde la pestaña **Actions** de cada run. El resumen de porcentajes también aparece directamente en el log del job en GitHub.

### Pruebas frontend (Vitest)

```bash
cd sistema-evaluacion-academica
npm run test          # una pasada
npm run test:watch    # modo watch
```

Cubren: `GradeBadge` (colores, formato), formulario de login (validación, submit, estados), `SubjectCard` (estados de inscripción, prerequisitos, conflictos).

<details>
<summary>Pruebas E2E (Playwright) — locales únicamente</summary>

Las pruebas E2E no corren en CI. Ver [por qué](#por-qué-playwright-no-está-en-ci).

**Prerrequisitos:** Docker + backend corriendo.

```bash
cd sistema-evaluacion-academica
npm run test:e2e        # headless (compatible CI local)
npm run test:e2e:ui     # modo UI interactivo con trazas
```

**Escenarios cubiertos:**

| Archivo | Escenarios |
|---------|------------|
| `auth.spec.ts` | Login Admin → redirect + bienvenida; contraseña vacía → error de validación |
| `dashboard.spec.ts` | Sidebar Admin; tarjetas de estadísticas con datos reales |
| `admin.spec.ts` | Página de profesores; página de materias; modales de creación |
| `students.spec.ts` | Tabla sembrada; búsqueda server-side; estado vacío |
| `professor.spec.ts` | Sidebar Profesor; página de calificaciones; modal de eliminar actividad |
| `student.spec.ts` | Catálogo de materias; filtro por carrera; estados de inscripción |

`playwright.config.ts` reutiliza un proceso Next.js en ejecución o lo inicia automáticamente. El backend no se inicia solo.

#### Por qué Playwright no está en CI

Cada flujo E2E (login, inscripción, calificaciones) ya está cubierto por las 259 pruebas de integración que **sí corren en CI** con Testcontainers. Añadir E2E en CI agrega 60-120 s de arranque (SQL Server + backend + browser) con riesgo de fallas intermitentes no relacionadas con el código bajo prueba.

</details>

---

## Decisiones técnicas

### `Result<T>` pattern

Los servicios devuelven `Result<T>` con código HTTP semántico en lugar de lanzar excepciones para fallos esperados (not found, validación, regla de negocio). Los caminos de error son explícitos en el sistema de tipos, los controladores son delgados — llaman al servicio, mapean el resultado y retornan. `ExceptionHandlingMiddleware` solo captura lo verdaderamente inesperado.

---

### Testcontainers para pruebas de integración

Cada ejecución levanta un SQL Server desechable, aplica migraciones y el seeder, ejecuta las pruebas y termina. Sin estado compartido entre ejecuciones de CI, sin dependencia de Docker Compose, y las pruebas ejercen el driver real de base de datos.

---

### Transacción Serializable en inscripción

`EnrollmentService.EnrollAsync` usa un patrón de dos fases: verificaciones previas baratas (sin lock) seguidas de reverificación bajo `IsolationLevel.Serializable`.

<details>
<summary>Por qué Serializable y no ReadCommitted</summary>

**El problema concreto:** dos estudiantes hacen clic en "inscribirse" al mismo tiempo. La sección tiene un cupo restante. Con `ReadCommitted`, ambas transacciones leen capacidad disponible, ambas pasan la validación y ambas hacen `INSERT` — sección sobreinscrita.

**Por qué `ReadCommitted` no alcanza:** permite non-repeatable reads. T1 y T2 leen el conteo antes de que cualquiera inserte, ambas ven cupo disponible, ambas confirman.

**Por qué el `UNIQUE CONSTRAINT` solo no es suficiente:** el constraint en `(StudentId, SectionId)` impide que *el mismo estudiante* se inscriba dos veces. Pero si 10 estudiantes distintos compiten por el último cupo, todos tienen `StudentId` diferente — el constraint no ayuda.

**El patrón de dos fases:**
1. **Verificaciones previas (sin lock):** baratas, rápidas, sin contención. Rechazan casos obviamente inválidos antes de adquirir recursos.
2. **Reverificación bajo `Serializable`:** dentro del lock se vuelve a consultar capacidad e inscripción duplicada. Si el estado cambió (otra transacción confirmó en el ínterin), rollback + error estructurado.

`IsolationLevel.Serializable` en SQL Server garantiza que ninguna otra transacción puede insertar un `SectionEnrollment` para la misma sección mientras la transacción actual está activa.

**Cómo el test confirma el diseño:** `Enroll_ConcurrentCapacityOne_ExactlyOneSucceeds` lanza 8 tareas simultáneas contra una sección con capacidad 1. Exactamente 1 debe tener éxito; las 7 restantes reciben 4xx estructurado. Sin mocks — SQL Server real vía Testcontainers.

</details>

---

### Verificación de prerequisitos

Todos los prerequisitos activos se cargan en una sola consulta y se agrupan en memoria — evita el N+1 que surgiría de verificar por materia en un bucle. La detección de ciclos usa DFS en el grafo dirigido `SubjectId → PrerequisiteSubjectId`.

---

### Materias multi-carrera

Una materia puede pertenecer a una carrera o a todas (`AppliesToAllCareers = true`). La lista de carreras se almacena como columna JSON en `Subjects`. Evita una tabla de unión para el caso común (materia de una sola carrera), soporta materias compartidas sin complejidad adicional.

---

### Carreras como constantes tipadas

Las tres carreras del sistema están definidas como constantes `string` en `CareerTypes` (`Domain/Enums/CareerType.cs`).

Esta decisión fue deliberada. Un catálogo dinámico requeriría tabla `Careers`, endpoints CRUD de administración, validación referencial en `Students` y `Subjects`, y migración de las carreras embebidas como JSON en `Subjects.Careers`. Esa complejidad no aporta nada al objetivo del proyecto.

Las constantes tienen además una ventaja concreta: el compilador detecta carreras inexistentes. Una tabla dinámica las convierte en errores de runtime.

---

### Dos sistemas de calificación con puente explícito

| Concepto | Tabla | Quién asigna | Para qué |
|----------|-------|--------------|----------|
| `ActivitySubmission.Score` | `ActivitySubmissions` | Profesor, por entrega | Retroalimentación y seguimiento LMS |
| `Grade.Value` | `Grades` | Profesor, vía `POST /api/grades` | Nota oficial del expediente académico |

La separación es intencional: las notas de actividades LMS y el registro académico son contextos con ciclos de vida distintos — las primeras se pueden editar durante el período; las oficiales son el registro final inmutable.

Para que los dos sistemas no sean "cajas negras" desconectadas, el servidor expone un puente explícito:

- `GET /api/courses/{sectionId}/grade-suggestions` (Profesor/Admin): por cada estudiante inscrito devuelve la **nota sugerida** (promedio ponderado de `ActivitySubmission.Score`, escala 0–10) y la **nota oficial** actual (`Grade.Value`). Permite al profesor ver el cálculo automático como referencia antes de asignar la nota oficial.
- `GET /api/courses/{sectionId}/my-grade-suggestion` (Estudiante): muestra al alumno su propio promedio de actividades y, si ya fue asignada, su calificación oficial.

El cálculo de nota sugerida es: `round(Σ(score/maxScore × weight) / totalWeight × 10, 2)`. Devuelve `null` si no hay entregas calificadas. **No hay conversión automática** — el profesor sigue siendo quien decide la nota oficial.

---

### Rate limiting y `AcademicSettings`

`FixedWindowRateLimiter` (5 req/min por IP) configurable vía `IOptions<RateLimitSettings>` — las pruebas de integración elevan el límite con `PostConfigure` sin reiniciar la app.

`MaxCreditsPerPeriod` (default: 24) se configura en `appsettings.json` e inyecta en `EnrollmentService` vía `IOptions<AcademicSettings>`. El valor por defecto está en la clase como seguridad — si falta la sección en la configuración, el sistema sigue funcionando.

```json
"AcademicSettings": {
  "MaxCreditsPerPeriod": 21
}
```

---

### X-Request-ID

Cada respuesta incluye el header `X-Request-ID`. Si el cliente lo envía, se propaga; si no, se genera un GUID. El mismo valor aparece en el header **y** en el campo `requestId` del body de error — permite correlacionar petición con línea de log.

```bash
curl -H "X-Request-ID: debug-123" http://localhost:5000/api/professors
# Respuesta: X-Request-ID: debug-123
# Error: { "statusCode": 404, "requestId": "debug-123", ... }
```

---

## ⚠️ Trade-offs conscientes

Estas son decisiones de scope tomadas deliberadamente para mantener el proyecto enfocado en los patrones arquitectónicos que demuestra. Ninguna es un olvido; cada una tiene una alternativa productiva documentada.

<details>
<summary><strong>JWT sin refresh tokens</strong> — expiración explícita en 8 h (producción) / 24 h (desarrollo)</summary>

El token de acceso vive en `localStorage` con una expiración de 8 horas en producción (24 h en entorno de desarrollo). Cuando expira, cualquier llamada devuelve 401; el cliente intercepta, limpia la sesión y redirige a `/login` con toast "Tu sesión expiró". No hay pérdida silenciosa de datos ni bucles de redirección.

`localStorage` es accesible desde JavaScript; el riesgo XSS existe. La mitigación implementada es una CSP estricta que bloquea scripts de orígenes externos. `'unsafe-inline'` sigue siendo necesario para Next.js App Router (transferencia de estado RSC).

**En producción:** token de acceso de 15 min en memoria, refresh token de 7–30 días en cookie `httpOnly; SameSite=Strict` con rotación en cada uso y revocación por familia ante token reutilizado.

</details>

<details>
<summary><strong>Sin observabilidad avanzada</strong> — logs + X-Request-ID, sin trazas distribuidas</summary>

Cada request recibe un `X-Request-ID` único propagado en logs y respuestas. Los logs de ASP.NET Core escriben a stdout con nivel configurable. No hay exportación de métricas ni trazas distribuidas.

**En producción:** OpenTelemetry SDK con exportadores a Jaeger/Tempo (trazas) y Prometheus/Grafana (métricas). Serilog con sinks a Seq o Elastic para búsqueda estructurada. Alertas sobre tasa de error y latencia p99 desde el día uno.

</details>

<details>
<summary><strong>E2E fuera de CI</strong> — cubierto por integración + QA manual</summary>

Los 259 tests de integración con Testcontainers cubren cada flujo de API contra SQL Server real: inscripción concurrente, prerequisitos, límite de créditos, períodos cerrados. La UI se valida manualmente según el QA_MANUAL.md. Añadir E2E en CI agrega 60–120 s de arranque de browser más el tiempo del backend, con riesgo de flakiness no relacionado con el código bajo prueba.

**En producción:** Playwright o Cypress en CI con servidor real o MSW para aislar la UI. Smoke tests de los happy paths en staging antes de cada deploy.

</details>

<details>
<summary><strong>Carreras como constantes</strong> — tabla en lugar de enumeración aplazada</summary>

Las tres carreras (`Ingeniería en Sistemas`, `Ciberseguridad`, `Desarrollo de Software`) están definidas como constantes en `CareerTypes` y validadas a nivel de servicio. La lógica de `AppliesToCareer` y los filtros del catálogo las tratan como strings conocidos.

**Por qué es suficiente aquí:** el scope del sistema es un demo con tres carreras fijas. Agregar una tabla `Careers` con FK en `Subject` y `Student` es la refactorización obvia pero no aporta nada al conjunto de patrones que el proyecto demuestra.

**En producción:** tabla `Careers` con `Id, Name, Code, IsActive`, FK en ambas entidades, endpoints CRUD para administración. La constante `CareerTypes` desaparece.

</details>

<details>
<summary><strong>Sin multi-tenant ni features enterprise</strong> — scope deliberado</summary>

El sistema modela una sola institución. No hay `TenantId` en las entidades, ni aislamiento por schema, ni gestión de roles por organización, ni SSO empresarial, ni exportación a SIS externos.

Estas características son las primeras que añadiría un cliente real, y su ausencia es la que hace que el proyecto sea legible como demostración de arquitectura. Un sistema multi-tenant correcto requiere decisiones de aislamiento (schema-per-tenant, row-level, o instancia separada) que dependen de requisitos de compliance que este scope no tiene.

</details>

<details>
<summary><strong>Coverage frontend parcial</strong></summary>

La lógica de negocio vive en el backend (474 unit + 259 integración). El frontend tiene tests Vitest/Testing Library para los componentes y flujos de mayor valor: `GradeBadge`, `LoginPage` y `SubjectCard`. Los componentes puramente presentacionales y las páginas de administración que requieren muchos mocks de TanStack Query no están cubiertos.

**En producción:** tests unitarios para hooks de estado complejo (`useEnrollment`, `useGrades`), snapshot tests para el sistema de diseño compartido, y coverage mínimo por threshold en CI para evitar regresiones silenciosas.

</details>

<details>
<summary><strong>Otras limitaciones conocidas</strong></summary>

**Backend**
- Sin HTTPS forzado en desarrollo local (Kestrel lo soporta; aplazado por simplicidad del setup)
- `SERIALIZABLE` en inscripción puede ser cuello de botella bajo carga muy alta — en producción se evaluaría lock optimista o cola de solicitudes con idempotency key
- Sin réplicas de lectura ni caché distribuido (Redis) para endpoints de alto volumen

**Frontend**
- Mutaciones disparan `refetch` completo; sin actualizaciones optimistas
- Sin tiempo real (WebSockets / SSE) — cambios en capacidad de sección no se reflejan hasta la siguiente navegación
- Sin subida de archivos en entregas de actividades (solo texto)
- Dashboards muestran agregados básicos (promedio, conteo); sin gráficas de tendencia temporal ni comparativas por cohorte

</details>

---

## Estructura del proyecto

```
.
├── docker-compose.yml               # SQL Server para desarrollo local
├── .env.example                     # Plantilla de variables de entorno
├── src/
│   ├── API/                         # Controllers · Middleware · Program.cs
│   ├── Application/                 # Services · DTOs · Interfaces · Result<T>
│   ├── Domain/                      # Entities · Domain Interfaces · Enums
│   └── Infrastructure/              # EF Core · Repos · JWT · Seeders
├── tests/
│   ├── UnitTests/                   # xUnit + Moq — sin dependencias externas
│   └── IntegrationTests/            # xUnit + Testcontainers — SQL Server real
└── sistema-evaluacion-academica/    # Frontend Next.js
```

---

## Comandos útiles

| Tarea | Comando |
|-------|---------|
| **Base de datos** | |
| Iniciar BD | `docker compose up -d` |
| Detener BD (conservar datos) | `docker compose stop` |
| Detener BD + eliminar datos | `docker compose down -v` |
| Ver logs | `docker compose logs sqlserver` |
| **Migraciones** | |
| Agregar migración | `dotnet ef migrations add <Nombre> --project src/Infrastructure --startup-project src/API` |
| Aplicar migraciones | `dotnet ef database update --project src/Infrastructure --startup-project src/API` |
| Revertir última | `dotnet ef migrations remove --project src/Infrastructure --startup-project src/API` |
| **Backend** | |
| Compilar | `dotnet build` |
| Ejecutar | `cd src/API && dotnet run` |
| Pruebas unitarias | `dotnet test tests/UnitTests --configuration Release` |
| Pruebas de integración | `dotnet test tests/IntegrationTests --configuration Release` |
| Todas las pruebas | `dotnet test --configuration Release` |
| Coverage backend | `dotnet test --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory coverage-results` |
| **Frontend** | |
| Lint | `cd sistema-evaluacion-academica && npm run lint` |
| Compilar | `cd sistema-evaluacion-academica && npm run build` |
| Pruebas componentes | `cd sistema-evaluacion-academica && npm run test` |
| Pruebas E2E | `cd sistema-evaluacion-academica && npm run test:e2e` |
| Pruebas E2E UI | `cd sistema-evaluacion-academica && npm run test:e2e:ui` |
