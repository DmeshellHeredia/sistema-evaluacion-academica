# Guía de Validación Manual — Sistema de Evaluación Académica

Este es un plan de pruebas, no un reporte de ejecución. Cada sección lista los escenarios a verificar con el resultado esperado para cada uno. Úsalo como guía al validar manualmente antes de un release.

---

## Leyenda de prioridades

| Ícono | Nivel | Significado |
|---|---|---|
| 🔴 | **Crítica** | Bloquea el deploy. Debe pasar sí o sí. |
| 🟠 | **Alta** | Funcionalidad principal. Debe pasar antes de publicar. |
| 🟡 | **Media** | Mejora de UX importante. Puede diferirse a hotfix rápido. |
| 🟢 | **Baja** | Detalle cosmético o edge case. Puede diferirse. |

---

## Usuarios demo de referencia

| Rol | Correo | Contraseña | Detalle |
|---|---|---|---|
| Admin | admin@academia.com | Admin123! | Acceso total |
| Profesor | prof.garcia@academia.com | Profesor123! | Prof. Ana García |
| Profesor 2 | prof.lopez@academia.com | Profesor123! | Prof. Roberto López |
| Profesor 3 | prof.martinez@academia.com | Profesor123! | Prof. Sofía Martínez |
| Estudiante IS sem3 | juan.perez@academia.com | Estudiante123! | 6 calificaciones, sem 1+2 aprobados |
| Estudiante IS sem1 | carlos.ruiz@academia.com | Estudiante123! | Pocas calificaciones, sem 1 |
| Estudiante CIB sem2 | lucia.torres@academia.com | Estudiante123! | sem 1 CIB aprobado |
| Estudiante DS sem3 | sofia.mendez@academia.com | Estudiante123! | sem 1+2 DS aprobados |

**Período activo sembrado:** Primer Semestre 2025 (código: 2025-1) — inscripción **abierta**

---

## 1. Setup Inicial

### 1.1 Prerrequisitos de entorno

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 1.1.1 | `dotnet --version` devuelve 8.x | 🔴 | `8.0.x` |
| 1.1.2 | `node --version` devuelve 20+ | 🔴 | `v20.x.x` o superior |
| 1.1.3 | `docker --version` devuelve versión válida | 🔴 | Versión de Docker |
| 1.1.4 | `dotnet tool list -g` incluye `dotnet-ef` | 🟠 | `dotnet-ef` en la lista |

---

### 1.2 Docker y base de datos

**Pasos:**
1. `docker compose up -d`
2. Esperar ~30 segundos
3. `docker compose ps`

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 1.2.1 | El contenedor `sqlserver` inicia sin errores | 🔴 | Status: `healthy` |
| 1.2.2 | El puerto 1444 está escuchando | 🔴 | `docker compose ps` → `0.0.0.0:1444->1433/tcp` |
| 1.2.3 | Reiniciar el contenedor y verificar que los datos persistan | 🟠 | `docker compose stop && docker compose up -d` → datos intactos |

---

### 1.3 Backend — inicio y migraciones

**Pasos:**
1. `cd src/API && dotnet run`
2. Observar la consola durante el arranque

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 1.3.1 | El backend arranca sin errores de migración | 🔴 | Log: migraciones aplicadas sin excepciones |
| 1.3.2 | El seeder imprime los usuarios demo en consola | 🔴 | Log: `[DatabaseSeeder] Datos académicos insertados.` con la lista de usuarios |
| 1.3.3 | La API responde en `http://localhost:5000/health` | 🔴 | HTTP 200 `{"status":"Healthy",...}` |
| 1.3.4 | Swagger UI carga en `http://localhost:5000` | 🟠 | Página Swagger con todos los endpoints visible |
| 1.3.5 | Segundo arranque no re-siembra datos (seeder es idempotente) | 🟠 | El seeder no inserta datos duplicados |

**Si SQL Server no está listo:**

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 1.3.6 | El backend reintenta 5 veces con mensaje claro si la BD no está disponible | 🟡 | Log: `SQL Server no responde (intento X/5). Reintentando en Xs...` |
| 1.3.7 | Tras 5 intentos fallidos, el backend termina mostrando el panel de ayuda con instrucciones | 🟡 | Panel ASCII con pasos: `docker compose up -d` etc. |

---

### 1.4 Frontend — setup

**Pasos:**
1. `echo "NEXT_PUBLIC_API_URL=http://localhost:5000" > sistema-evaluacion-academica/.env.local`
2. `cd sistema-evaluacion-academica && npm install`
3. `npm run dev`

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 1.4.1 | `npm install` completa sin errores críticos | 🔴 | Finaliza con `added X packages` |
| 1.4.2 | `npm run dev` arranca Next.js sin errores | 🔴 | `Ready - started server on 0.0.0.0:3000` |
| 1.4.3 | `http://localhost:3000` redirige a `/login` si no está autenticado | 🔴 | Redirige automáticamente a `/login` |
| 1.4.4 | La página de login carga sin errores de consola | 🟠 | Sin errores en DevTools Console |
| 1.4.5 | El archivo `.env.local` tiene `NEXT_PUBLIC_API_URL=http://localhost:5000` | 🔴 | Confirmado en archivo |

---

## 2. Login / Autenticación

### 2.1 Login válido

**Precondición:** Backend y frontend corriendo. Todos los usuarios demo sembrados.

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 2.1.1 | Login con `admin@academia.com` / `Admin123!` | 🔴 | Redirige a `/dashboard`. Nombre del usuario visible en header. |
| 2.1.2 | Login con `prof.garcia@academia.com` / `Profesor123!` | 🔴 | Redirige a `/dashboard`. Sidebar muestra ítems de Profesor. |
| 2.1.3 | Login con `juan.perez@academia.com` / `Estudiante123!` | 🔴 | Redirige a `/dashboard`. Sidebar muestra ítems de Estudiante. |
| 2.1.4 | El JWT se almacena en `localStorage` tras login exitoso | 🟠 | DevTools → Application → localStorage → clave `academia_token` con valor |
| 2.1.5 | El rol del usuario se refleja correctamente en el sidebar | 🟠 | Admin ve todos los ítems. Profesor no ve "Estudiantes" ni "Inscripciones". Estudiante solo ve sus secciones. |

---

### 2.2 Login inválido

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 2.2.1 | Email correcto + contraseña incorrecta | 🔴 | Toast de error o mensaje en formulario. Sin redirección. |
| 2.2.2 | Email inexistente + cualquier contraseña | 🔴 | Mismo error genérico que 2.2.1 (sin revelar si el email existe) |
| 2.2.3 | Campos vacíos — intentar submit | 🟠 | Validación de formulario impide el envío. Mensaje visible. |
| 2.2.4 | Email sin formato de email (`notanemail`) | 🟠 | Validación de formato o error 422 de API |
| 2.2.5 | La contraseña NO aparece en texto plano en pantalla | 🟠 | Campo tipo `password`, texto enmascarado |

---

### 2.3 Logout

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 2.3.1 | Hacer logout desde el menú de usuario | 🔴 | Redirige a `/login`. Token eliminado de `localStorage`. |
| 2.3.2 | Tras logout, intentar navegar a `/dashboard` manualmente | 🔴 | Redirige a `/login` sin mostrar datos |
| 2.3.3 | Tras logout, el token en `localStorage` ya no existe | 🟠 | DevTools → localStorage → clave `academia_token` ausente |

---

### 2.4 Rutas protegidas

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 2.4.1 | Sin sesión: acceder a `http://localhost:3000/dashboard` | 🔴 | Redirige a `/login` |
| 2.4.2 | Sin sesión: acceder a `http://localhost:3000/dashboard/students` | 🔴 | Redirige a `/login` |
| 2.4.3 | Sin sesión: `GET http://localhost:5000/api/students` (sin token) | 🔴 | HTTP 401 con `{"statusCode":401,"message":"Token JWT requerido o inválido."}` |
| 2.4.4 | Estudiante autenticado: acceder a `/dashboard/students` | 🔴 | La página no carga datos de estudiantes o redirige (403 en API) |

---

### 2.5 Expiración de sesión

**Nota:** En Development el JWT dura 24 horas (1440 min). Para probar expiración, modificar `ExpirationMinutes` a `1` en `appsettings.Development.json` y reiniciar el backend.

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 2.5.1 | Al recibir un 401 por token vencido, el cliente redirige a `/login` | 🔴 | Redirección automática sin acción del usuario |
| 2.5.2 | En `/login` tras expiración, aparece toast "Tu sesión expiró. Inicia sesión nuevamente." | 🔴 | Toast naranja/warning visible al cargar `/login` |
| 2.5.3 | El toast de expiración aparece solo una vez (no en recargas posteriores de `/login`) | 🟠 | Segunda carga de `/login` → sin toast |
| 2.5.4 | Múltiples llamadas concurrentes con token vencido generan solo una redirección | 🟠 | Una sola redirección, no loop ni múltiples navigations |
| 2.5.5 | Un 403 (prohibido) NO redirige a `/login` — deja al usuario en la página actual | 🟠 | El error 403 se muestra en UI pero no desloguea |

---

### 2.6 Rate limiting en login

**Nota:** En Development el límite es 100 req/min. Para probar con 5/min, setear `RateLimit:LoginPermitLimit=5` o probar en producción.

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 2.6.1 | 6 intentos de login rápidos con límite configurado en 5 → el 6to devuelve 429 | 🟠 | HTTP 429 `{"statusCode":429,"message":"Demasiados intentos de login..."}` |
| 2.6.2 | La respuesta 429 incluye cabecera `Retry-After` | 🟠 | `Retry-After: 60` (o valor cercano) en las cabeceras |
| 2.6.3 | Otro IP distinto no es bloqueado cuando el primero alcanza el límite | 🟡 | Petición con IP diferente → 401 (no 429) |

---

## 3. Flujo ADMIN

**Precondición para toda esta sección:** Sesión activa como `admin@academia.com`.

---

### 3.1 Dashboard de Admin

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.1.1 | El dashboard carga sin errores | 🔴 | Página visible sin errores en consola |
| 3.1.2 | Las tarjetas de estadísticas muestran conteos correctos | 🟠 | 4 estudiantes, 3 profesores, ~26 materias, ~26 secciones |
| 3.1.3 | El sidebar muestra todos los ítems de Admin | 🟠 | Visibles: Dashboard, Estudiantes, Profesores, Materias, Secciones, Períodos, Calificaciones, Mis Cursos (u opciones Admin) |

---

### 3.2 Gestión de estudiantes

#### 3.2.1 Listar estudiantes

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.2.1.1 | La tabla muestra los 4 estudiantes sembrados | 🔴 | Juan Pérez, Carlos Ruiz, Lucía Torres, Sofía Méndez visibles |
| 3.2.1.2 | La paginación funciona (navegar páginas) | 🟠 | Botones anterior/siguiente funcionan |
| 3.2.1.3 | Búsqueda por nombre filtra correctamente | 🟠 | Buscar "Juan" → solo Juan Pérez aparece |
| 3.2.1.4 | Búsqueda sin resultados muestra estado vacío | 🟠 | Buscar "xyzno" → mensaje "No se encontraron estudiantes" o equivalente |
| 3.2.1.5 | Limpiar búsqueda restaura la lista completa | 🟡 | Borrar el campo → lista completa vuelve |

#### 3.2.2 Crear estudiante

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.2.2.1 | Crear estudiante con datos válidos (nombre, apellido, carrera, semestre, contraseña) | 🔴 | Estudiante creado, aparece en tabla, toast "Estudiante creado" |
| 3.2.2.2 | El correo generado automáticamente sigue el patrón `nombre.apellido@academia.com` | 🟠 | ej. "pedro.garcia@academia.com" para "Pedro García" |
| 3.2.2.3 | Intentar crear estudiante con nombre que derive el mismo correo de uno existente | 🟠 | Error: "El correo electrónico ya está registrado." |
| 3.2.2.4 | Campos obligatorios vacíos → formulario no se envía | 🟠 | Validación visual antes del POST |
| 3.2.2.5 | Semestre con valor inválido (ej: 0 o texto) → rechazo | 🟡 | Error de validación |

#### 3.2.3 Editar y eliminar estudiante

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.2.3.1 | Editar el semestre de Carlos Ruiz de 1 a 2 | 🟠 | Cambio reflejado en tabla. Toast de éxito. |
| 3.2.3.2 | Eliminar un estudiante recién creado (no los demo) | 🟠 | Estudiante desaparece de la lista (soft delete). Toast de éxito. |
| 3.2.3.3 | El estudiante eliminado ya no puede hacer login | 🟡 | Login devuelve 401 |

---

### 3.3 Gestión de profesores

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.3.1 | Lista de profesores muestra los 3 sembrados | 🔴 | Ana García, Roberto López, Sofía Martínez |
| 3.3.2 | Crear profesor con datos válidos | 🔴 | Profesor creado, aparece en lista, toast de éxito |
| 3.3.3 | Intentar crear profesor con correo ya existente | 🟠 | Error: "El correo electrónico ya está registrado." |
| 3.3.4 | Editar nombre de un profesor | 🟠 | Nombre actualizado en lista |
| 3.3.5 | Eliminar profesor recién creado | 🟠 | Soft delete. Desaparece de lista. |

---

### 3.4 Gestión de materias

#### 3.4.1 Listar y buscar

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.4.1.1 | La tabla muestra las ~26 materias sembradas | 🔴 | Materias de IS, CIB y DS visibles |
| 3.4.1.2 | Búsqueda por código (ej: "IS-MAT") filtra correctamente | 🟠 | Solo materias IS-MAT1 e IS-MAT2 |
| 3.4.1.3 | Búsqueda por nombre parcial funciona | 🟠 | Buscar "Programación" → todas las materias de Programación |
| 3.4.1.4 | Paginación funciona con pageSize pequeño | 🟡 | Cambiar pageSize o navegar páginas |

#### 3.4.2 Crear materia regular (una carrera)

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.4.2.1 | Crear materia con código único, nombre, descripción, créditos y semestre | 🔴 | Materia creada, aparece en tabla |
| 3.4.2.2 | Intentar crear materia con código duplicado | 🟠 | Error indicando código duplicado |
| 3.4.2.3 | Campos obligatorios vacíos → rechazo con validación | 🟠 | Errores de validación visibles |
| 3.4.2.4 | Créditos con valor 0 o negativo → rechazo | 🟡 | Error de validación |

#### 3.4.3 Crear materia multi-carrera (AppliesToAllCareers)

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.4.3.1 | Crear materia marcando "Aplica a todas las carreras" | 🔴 | Materia creada con `appliesToAllCareers: true`. Visible para estudiantes de IS, CIB y DS en el catálogo. |
| 3.4.3.2 | La materia multi-carrera aparece en el catálogo de un estudiante IS | 🔴 | Login como Juan Pérez → catálogo → materia visible |
| 3.4.3.3 | La materia multi-carrera aparece en el catálogo de un estudiante CIB | 🔴 | Login como Lucía Torres → catálogo → materia visible |
| 3.4.3.4 | La materia multi-carrera aparece en el catálogo de un estudiante DS | 🟠 | Login como Sofía Méndez → catálogo → materia visible |

#### 3.4.4 Prerequisitos

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.4.4.1 | Ver prerequisitos de IS-MAT2 → debe mostrar IS-MAT1 | 🔴 | Lista: IS-MAT1 Matemáticas I |
| 3.4.4.2 | Ver prerequisitos de CIB-HAK1 → debe mostrar CIB-CRP1 y CIB-RED2 | 🔴 | Dos prerequisitos listados |
| 3.4.4.3 | Agregar prerequisito a una materia | 🟠 | Prerequisito agregado, visible en lista |
| 3.4.4.4 | Eliminar un prerequisito | 🟠 | Prerequisito eliminado de la lista |
| 3.4.4.5 | Intentar crear ciclo de prerequisitos (A requiere B, B requiere A) | 🔴 | Error: "Dependencia circular detectada" o equivalente |
| 3.4.4.6 | Intentar que una materia sea prerequisito de sí misma | 🔴 | Error: "Una materia no puede ser prerequisito de sí misma" o equivalente |
| 3.4.4.7 | Reemplazar todos los prerequisitos con un set vacío | 🟠 | Materia queda sin prerequisitos |

#### 3.4.5 Soft delete de materia

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.4.5.1 | Eliminar una materia recién creada | 🟠 | Desaparece de la lista (soft delete, no DELETE real) |
| 3.4.5.2 | La materia eliminada ya no aparece en el catálogo de estudiantes | 🟠 | No visible en catálogo |
| 3.4.5.3 | Estudiantes ya inscritos en sección de materia eliminada no pueden ver esa sección activa | 🟡 | Sección marcada como no disponible |

---

### 3.5 Gestión de secciones

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.5.1 | Listar secciones → ~26 secciones sembradas visibles | 🔴 | Lista completa con paginación |
| 3.5.2 | Búsqueda por código de materia (ej: "IS-MAT") filtra secciones | 🟠 | Solo secciones de IS-MAT1 e IS-MAT2 |
| 3.5.3 | Crear sección para materia existente con profesor asignado | 🔴 | Sección creada. Código de sección "B" si "A" ya existe para esa materia. |
| 3.5.4 | Crear sección con capacidad 0 → rechazo | 🟡 | Error de validación |
| 3.5.5 | Crear sección sin asignar profesor → rechazo | 🟠 | Error de validación |
| 3.5.6 | Editar horario de una sección (día, hora inicio/fin) | 🟠 | Cambio reflejado en lista |
| 3.5.7 | Editar capacidad de una sección | 🟠 | Capacidad actualizada |
| 3.5.8 | Eliminar sección recién creada (sin inscripciones) | 🟠 | Sección desaparece (soft delete) |
| 3.5.9 | Ver estudiantes inscritos en una sección desde la lista Admin | 🟠 | Lista de estudiantes con nombre, código y calificación actual |
| 3.5.10 | Paginación de secciones funciona | 🟡 | Navegar páginas sin error |

---

### 3.6 Períodos académicos

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.6.1 | La página de períodos muestra el período "Primer Semestre 2025" activo | 🔴 | Banner verde: "Selección activa — Primer Semestre 2025 (2025-1)" |
| 3.6.2 | Crear nuevo período con nombre, código y fechas válidas | 🔴 | Período creado, aparece en lista |
| 3.6.3 | Intentar crear período con código duplicado | 🟠 | Error de validación |
| 3.6.4 | Abrir inscripción en nuevo período → cierra el activo automáticamente | 🔴 | Solo un período puede estar abierto a la vez. El anterior se cierra. |
| 3.6.5 | Cerrar inscripción del período activo | 🔴 | Banner cambia a "La selección de materias está desactivada" |
| 3.6.6 | Con inscripción cerrada, un estudiante no puede inscribirse | 🔴 | POST /api/enrollments → 400 "La selección de materias no está activa actualmente." |
| 3.6.7 | Intentar eliminar un período con inscripción abierta | 🟠 | Botón desactivado o error: "No se puede eliminar un período activo" |
| 3.6.8 | Editar nombre y fechas de un período cerrado | 🟡 | Cambio guardado correctamente |
| 3.6.9 | Eliminar un período cerrado sin inscripciones | 🟠 | Período eliminado de la lista |

---

### 3.7 Calificaciones (Admin)

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 3.7.1 | Ver calificaciones de Juan Pérez (juan.perez) → 6 calificaciones | 🔴 | 6 calificaciones visibles: IS-MAT1 (9.0), IS-PRG1 (8.5), IS-ALG1 (7.5), IS-MAT2 (8.0), IS-PRG2 (9.0), IS-BD1 (8.5) |
| 3.7.2 | Registrar calificación a Carlos Ruiz en IS-ALG1 | 🔴 | Calificación creada, visible en historial |
| 3.7.3 | Registrar calificación con nota 6 (reprobada) | 🟠 | Nota registrada, badge "Reprobado" visible |
| 3.7.4 | Registrar calificación con nota 10 (máxima) | 🟠 | Nota registrada correctamente |
| 3.7.5 | Registrar calificación con nota 11 (fuera de rango) → rechazo | 🟠 | Error de validación (nota máxima 10) |
| 3.7.6 | Registrar calificación con nota negativa → rechazo | 🟠 | Error de validación |
| 3.7.7 | Actualizar una calificación existente | 🟠 | Valor actualizado en el historial |
| 3.7.8 | Eliminar una calificación (soft delete) | 🟠 | Calificación desaparece del historial |
| 3.7.9 | El promedio del estudiante se recalcula correctamente tras nueva nota | 🟠 | Página de promedios muestra valor actualizado |

---

## 4. Flujo PROFESOR

**Precondición:** Sesión activa como `prof.garcia@academia.com`.

---

### 4.1 Vista de secciones del profesor

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 4.1.1 | Prof. García solo ve sus propias secciones | 🔴 | Solo secciones donde `professorId` = Prof. García. IS-MAT1, IS-ALG1, IS-MAT2, DS-PRG1, DS-WEB1, DS-PRG2, DS-WEB2 |
| 4.1.2 | Prof. García no ve secciones de Prof. López ni Prof. Martínez | 🔴 | Secciones de otros profesores no aparecen |
| 4.1.3 | La lista de "Mis cursos" muestra las secciones asignadas | 🟠 | Ítems en el sidebar con las secciones |
| 4.1.4 | Intentar acceder a la sección de otro profesor por URL directa → 403 | 🔴 | Error 403 o redirección sin datos |

---

### 4.2 Vista de estudiantes en sección

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 4.2.1 | En sección IS-MAT1-A, ver lista de estudiantes inscritos | 🔴 | Juan Pérez (EST-2025-A001) y Carlos Ruiz (EST-2025-A002) visibles |
| 4.2.2 | La lista muestra nombre, código de estudiante y calificación actual | 🟠 | Datos completos por cada estudiante |
| 4.2.3 | Estudiante sin calificación aparece con "Sin nota" o guion | 🟡 | Estado "Sin nota" visible para estudiantes sin calificación en esa sección |

---

### 4.3 Actividades

#### 4.3.1 Crear actividad

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 4.3.1.1 | Crear actividad tipo "Tarea" con título, descripción, fecha límite y puntuación | 🔴 | Actividad creada, visible en lista de actividades de la sección |
| 4.3.1.2 | Crear actividad tipo "Examen" | 🟠 | Actividad tipo Examen creada |
| 4.3.1.3 | Crear actividad tipo "Recurso" con URL | 🟠 | Actividad tipo Recurso con URL visible |
| 4.3.1.4 | Crear actividad tipo "Proyecto" | 🟡 | Actividad tipo Proyecto creada |
| 4.3.1.5 | Crear actividad sin fecha límite | 🟠 | Actividad creada sin fecha. Sin error. |
| 4.3.1.6 | Crear actividad con ponderación > 100 → rechazo | 🟡 | Error de validación |
| 4.3.1.7 | Actividad marcada como "No publicar" no visible para estudiantes | 🟠 | Login como estudiante → actividad no publicada ausente |

#### 4.3.2 Editar actividad

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 4.3.2.1 | Editar título de una actividad existente | 🟠 | Título actualizado, visible inmediatamente |
| 4.3.2.2 | Cambiar fecha límite de una actividad | 🟠 | Fecha actualizada |
| 4.3.2.3 | Publicar actividad que estaba sin publicar | 🟠 | Ahora visible para estudiantes |

#### 4.3.3 Eliminar actividad

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 4.3.3.1 | Eliminar actividad recién creada | 🟠 | Actividad desaparece de la lista |
| 4.3.3.2 | Modal de confirmación de borrado aparece antes de eliminar | 🟡 | Diálogo de confirmación visible |
| 4.3.3.3 | Profesor no puede eliminar actividades de otro profesor | 🔴 | Error 403 |

---

### 4.4 Anuncios

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 4.4.1 | Crear anuncio con título y contenido | 🔴 | Anuncio creado, visible en sección |
| 4.4.2 | Anuncio visible para estudiantes inscritos | 🔴 | Login como Juan Pérez → sección IS-MAT1 → anuncio visible |
| 4.4.3 | Editar título/contenido de un anuncio | 🟠 | Cambio guardado y visible |
| 4.4.4 | Eliminar un anuncio | 🟠 | Anuncio desaparece |
| 4.4.5 | Anuncio muestra nombre del autor y fecha de creación | 🟡 | "Ana García · 19 may 2026" o equivalente |
| 4.4.6 | Profesor no puede editar anuncios de otro profesor | 🔴 | Error 403 al intentar editar |

---

### 4.5 Calificaciones (Profesor)

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 4.5.1 | Prof. García puede registrar calificación para estudiante en su sección | 🔴 | Calificación registrada con éxito |
| 4.5.2 | Prof. García no puede registrar calificación en sección de otro profesor | 🔴 | Error 403 o "No tienes acceso..." |
| 4.5.3 | Ver promedio de una sección | 🟠 | Promedio calculado correctamente para la sección |

---

### 4.6 Calificación de entregas (Submissions)

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 4.6.1 | Ver lista de entregas de una actividad | 🔴 | Modal con entregas de estudiantes y sus contenidos |
| 4.6.2 | Calificar una entrega con nota válida y retroalimentación | 🔴 | Calificación guardada. Retroalimentación visible para el estudiante. |
| 4.6.3 | Calificar con nota mayor a la puntuación máxima → rechazo | 🟠 | Error de validación |
| 4.6.4 | La retroalimentación es opcional (dejar vacía y guardar) | 🟠 | Calificación guardada sin retroalimentación |
| 4.6.5 | El conteo de entregas calificadas/totales se actualiza tras calificar | 🟡 | ej. "1/2 calificadas" → "2/2 calificadas" |

---

## 5. Flujo ESTUDIANTE

**Precondición principal:** `juan.perez@academia.com` (Estudiante IS sem 3) a menos que se indique otro.

---

### 5.1 Catálogo de materias

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.1.1 | El catálogo muestra solo materias de la carrera del estudiante (IS) | 🔴 | Solo materias IS-xxx visibles |
| 5.1.2 | Las materias de sem 1 y 2 de Juan aparecen con estado "Aprobada" | 🔴 | IS-MAT1, IS-PRG1, IS-ALG1 → badge "Aprobada". IS-MAT2, IS-PRG2, IS-BD1 → badge "Aprobada" |
| 5.1.3 | Las materias de sem 3 inscritas aparecen con estado "Inscrita" | 🔴 | IS-RED1, IS-SO1, IS-BD2 → badge "Inscrita" |
| 5.1.4 | Materia aprobada no muestra botón de inscripción | 🔴 | Botón de inscripción ausente o deshabilitado en materias aprobadas |
| 5.1.5 | Login como Carlos Ruiz (sem 1): materias de sem 2+ aparecen como "Semestre insuficiente" | 🔴 | IS-MAT2 y todas las de sem 2+ → estado "Semestre insuficiente" o "Semestre previo" |

---

### 5.2 Inscripción — flujo happy path

**Precondición:** Período "Primer Semestre 2025" abierto. Usar `carlos.ruiz@academia.com` (sem 1 IS, con cupo disponible).

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.2.1 | Carlos Ruiz puede inscribirse en IS-ALG1 (sem 1, sin prerequisitos) | 🔴 | Toast "Inscripción realizada exitosamente." Estado cambia a "Inscrita" |
| 5.2.2 | Tras inscripción, la sección aparece en el horario del estudiante | 🔴 | Sección IS-ALG1-A visible en `/dashboard/schedule` |
| 5.2.3 | Desinscribirse de IS-ALG1 exitosamente | 🔴 | Toast de éxito. Estado vuelve a "Disponible" en catálogo. |
| 5.2.4 | Horario actualizado tras desinscripción | 🟠 | IS-ALG1 ya no aparece en horario |

---

### 5.3 Restricciones de inscripción

#### 5.3.1 Prerequisitos no cumplidos

**Actor:** `carlos.ruiz@academia.com` (sem 1, sin calificaciones de IS-MAT1)

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.3.1.1 | Carlos Ruiz intenta inscribirse en IS-MAT2 (requiere IS-MAT1 aprobada) | 🔴 | Error: "Prerequisitos no cumplidos: IS-MAT1 (Matemáticas I). Se requiere nota >= 7." |
| 5.3.1.2 | El catálogo muestra IS-MAT2 con estado "Bloqueada" para Carlos | 🔴 | Badge "Bloqueada" o "Prerequisito pendiente" |
| 5.3.1.3 | La lista de prerequisitos faltantes es visible en el catálogo | 🟠 | Tooltip o texto: "Requiere: IS-MAT1 Matemáticas I" |

#### 5.3.2 Choque de horario

**Actor:** Cualquier estudiante con una sección ya inscrita el mismo día/hora.

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.3.2.1 | Inscribir estudiante en dos materias con horario solapado | 🔴 | Error con detalle: "Choque de horario: 'IS-XXX' (Lunes 08:00-10:00) conflicta con 'IS-YYY' (Lunes 08:00-10:00)." |
| 5.3.2.2 | El catálogo muestra las secciones en conflicto con estado "Conflicto de horario" | 🟠 | Badge o indicador de conflicto visible |

#### 5.3.3 Límite de créditos

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.3.3.1 | Intentar inscribirse cuando los créditos actuales + nuevos superen 24 | 🔴 | Error: "Límite de carga académica: máximo 24 créditos por período. Ya tienes X crédito(s) inscritos y esta materia suma Y más." |

#### 5.3.4 Materia ya aprobada

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.3.4.1 | Juan Pérez intenta inscribirse en IS-MAT1 (ya aprobada con 9.0) | 🔴 | Error: "El estudiante ya aprobó esta materia." |
| 5.3.4.2 | IS-MAT1 aparece con estado "Aprobada" en el catálogo, botón de inscripción ausente | 🔴 | Badge "Aprobada" con la nota. Sin botón de inscripción. |

#### 5.3.5 Período cerrado

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.3.5.1 | Admin cierra el período activo. Estudiante intenta inscribirse. | 🔴 | Error: "La selección de materias no está activa actualmente." |
| 5.3.5.2 | El catálogo muestra aviso de que la selección está cerrada | 🟠 | Banner o texto indicando el cierre de inscripciones |

#### 5.3.6 Materia de otra carrera

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.3.6.1 | Juan Pérez (IS) intenta inscribirse en CIB-FUN1 (Ciberseguridad) por API directa | 🔴 | Error 400: "La materia 'Fundamentos de Ciberseguridad' no aplica a la carrera 'IngenieriaEnSistemas'." |

---

### 5.4 Horario del estudiante

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.4.1 | La página de horario muestra todas las secciones inscritas de Juan Pérez | 🔴 | 9 secciones (sem 1, 2 y 3 de IS) visibles |
| 5.4.2 | Cada sección muestra: materia, código de sección, profesor, día, horario, modalidad, aula | 🟠 | Todos los campos visibles |
| 5.4.3 | El período activo aparece en el encabezado del horario | 🟡 | "Primer Semestre 2025" visible |

---

### 5.5 Actividades (vista Estudiante)

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.5.1 | Estudiante ve actividades publicadas de su sección | 🔴 | Actividades listadas en la vista de la sección |
| 5.5.2 | Estudiante NO ve actividades no publicadas | 🔴 | Actividades sin publicar ausentes para el estudiante |
| 5.5.3 | Estudiante puede entregar una actividad (texto) | 🔴 | Modal de entrega acepta texto. Toast de éxito. Estado cambia. |
| 5.5.4 | Estudiante puede actualizar su entrega antes del cierre | 🟠 | Entrega actualizada correctamente |
| 5.5.5 | Actividad tipo "Recurso" muestra botón de descarga (URL), no de entrega | 🟠 | Botón "Descargar" en vez de "Entregar" |
| 5.5.6 | Tras calificación, el estudiante ve su nota y retroalimentación | 🔴 | Nota visible: "Calificación: X/Y" + feedback del profesor |
| 5.5.7 | Actividad vencida muestra indicador visual "Vencida" | 🟡 | Texto en rojo "Venció: DD/MM" |
| 5.5.8 | Actividad cerrada no permite enviar nueva entrega | 🟠 | Formulario de entrega oculto o desactivado con mensaje "Esta actividad está cerrada." |

---

### 5.6 Calificaciones (vista Estudiante)

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.6.1 | Página de calificaciones muestra el historial de Juan Pérez | 🔴 | 6 calificaciones visibles con detalle |
| 5.6.2 | El promedio general es correcto: (9.0+8.5+7.5+8.0+9.0+8.5)/6 = 8.42 | 🔴 | ~8.42 (o valor calculado por el sistema) |
| 5.6.3 | Calificación ≥ 9 → categoría "Excelente" | 🟠 | IS-MAT1 (9.0) y IS-PRG2 (9.0) muestran "Excelente" o color correspondiente |
| 5.6.4 | Calificación entre 7 y 8.99 → categoría "Bien" o "Muy bien" | 🟠 | IS-ALG1 (7.5) muestra categoría correcta |
| 5.6.5 | Calificación < 7 → categoría "Reprobado" | 🟠 | Una nota con valor 5 mostraría "Reprobado" |
| 5.6.6 | Estudiante no puede ver calificaciones de otro estudiante | 🔴 | Intentar `GET /api/grades/student/{otroEstudianteId}` → 403 |

---

### 5.7 Permisos del Estudiante

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 5.7.1 | Estudiante no puede acceder a `/dashboard/students` (gestión de estudiantes) | 🔴 | 403 en API o redirección sin datos |
| 5.7.2 | Estudiante no puede crear/editar/eliminar materias | 🔴 | `POST /api/subjects` → 403 |
| 5.7.3 | Estudiante no puede registrar calificaciones | 🔴 | `POST /api/grades` → 403 |
| 5.7.4 | Estudiante no puede acceder a períodos académicos | 🔴 | `GET /api/academic-periods` → 403 |
| 5.7.5 | Estudiante solo puede inscribirse a sí mismo (no a otro estudiante) | 🔴 | `POST /api/enrollments` con studentId de otro → 403 |
| 5.7.6 | Estudiante solo puede desinscribirse a sí mismo | 🔴 | `DELETE /api/enrollments/{otroId}/{seccionId}` → 403 |
| 5.7.7 | Estudiante no puede desinscribirse si ya tiene calificación en la materia | 🔴 | Error: "No se puede desinscribir de una materia que ya tiene calificación." |

---

## 6. Concurrencia

**Precondición:** Backend corriendo. Herramienta de prueba recomendada: curl, Postman, o similar para enviar peticiones en paralelo.

---

### 6.1 Doble inscripción simultánea

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 6.1.1 | Enviar 2 peticiones `POST /api/enrollments` idénticas (mismo estudiante + sección) en paralelo | 🔴 | Exactamente una solicitud exitosa (200). La otra devuelve 400/409 "El estudiante ya está inscrito en esta sección." |
| 6.1.2 | El segundo request concurrente no genera duplicados en DB | 🔴 | En DB solo existe una inscripción activa para esa combinación estudiante-sección |
| 6.1.3 | El mensaje de error es claro y no expone detalles de implementación (sin stack trace) | 🟠 | JSON limpio con `errorMessage` descriptivo |

---

### 6.2 Sección al límite de capacidad

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 6.2.1 | Crear sección con capacidad 1. Inscribir primer estudiante → éxito. Inscribir segundo → error. | 🔴 | Primero: 200 OK. Segundo: 400 "La sección '...' ha alcanzado su capacidad máxima." |
| 6.2.2 | Enviar 2 peticiones simultáneas para la última plaza disponible | 🔴 | Solo una inscripción confirmada. La otra rechazada con mensaje de capacidad máxima. |

---

### 6.3 Mensajes de error correctos bajo concurrencia

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 6.3.1 | Los errores de concurrencia devuelven JSON estructurado, no HTML ni stack traces | 🔴 | `{"errorMessage": "..."}` |
| 6.3.2 | Los errores 400/409 de concurrencia no exponen SQL ni detalles internos | 🔴 | Sin menciones a SQL Server, índices, ni códigos de error internos |

---

## 7. Frontend / UI

---

### 7.1 Estados de carga (loading states)

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 7.1.1 | Tablas con datos muestran skeleton/spinner mientras cargan | 🟠 | Animación visible durante la carga inicial |
| 7.1.2 | Botones de acción se deshabilitan mientras la petición está en curso | 🔴 | Botón "Guardar", "Crear", "Eliminar" deshabilitados durante el request |
| 7.1.3 | El botón submit muestra spinner durante el envío | 🟡 | Ícono giratorio visible en el botón |
| 7.1.4 | El botón de activar/desactivar período muestra spinner mientras procesa | 🟡 | Spinner visible en el botón "Activar"/"Desactivar" |

---

### 7.2 Estados vacíos (empty states)

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 7.2.1 | Tabla de estudiantes vacía muestra mensaje apropiado | 🟠 | Ícono + "No hay estudiantes" o similar |
| 7.2.2 | Lista de actividades sin actividades muestra empty state | 🟠 | Ícono + "Sin actividades" |
| 7.2.3 | Lista de anuncios sin anuncios muestra empty state | 🟠 | Ícono + "Sin anuncios" |
| 7.2.4 | Horario sin secciones muestra mensaje apropiado | 🟠 | Mensaje indicando que no hay materias inscritas |
| 7.2.5 | Búsqueda sin resultados muestra estado vacío (no tabla vacía sin mensaje) | 🟠 | Texto claro: "No se encontraron resultados para '...'" |

---

### 7.3 Estados de error (error states)

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 7.3.1 | Con el backend apagado, las páginas muestran error-state descriptivo | 🔴 | Componente de error con botón "Reintentar" y mensaje "Sin conexión con el servidor" |
| 7.3.2 | El botón "Reintentar" en el error-state vuelve a intentar la carga | 🟠 | Al hacer clic se dispara nuevo fetch |
| 7.3.3 | Los errores del servidor (500) muestran mensaje genérico sin exponer detalles | 🔴 | "Error del servidor. Intenta de nuevo." sin stack trace |
| 7.3.4 | Los errores de validación del formulario se muestran cerca del campo con el problema | 🟠 | Texto rojo junto al campo o bajo el botón submit |

---

### 7.4 Toasts / notificaciones

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 7.4.1 | Crear registro exitoso → toast verde de éxito | 🟠 | "Estudiante creado", "Período creado", etc. |
| 7.4.2 | Operación fallida → toast rojo con mensaje de error | 🟠 | Mensaje descriptivo del error |
| 7.4.3 | Toast de sesión expirada es de tipo warning (naranja) | 🟡 | Color diferente al error genérico |
| 7.4.4 | Los toasts desaparecen automáticamente tras unos segundos | 🟡 | Auto-dismiss en ~3-5 segundos |

---

### 7.5 Modales

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 7.5.1 | Los modales se cierran con la tecla Escape | 🟠 | Presionar Escape cierra el modal |
| 7.5.2 | Los modales se cierran haciendo clic fuera del contenido | 🟠 | Clic en el overlay oscuro cierra el modal |
| 7.5.3 | Los modales tienen título descriptivo visible | 🟠 | h2 con título visible dentro del modal |
| 7.5.4 | El modal de confirmación de borrado tiene botón "Cancelar" prominente | 🟡 | Botón "Cancelar" visible y funcional |

---

### 7.6 Navegación

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 7.6.1 | El sidebar resalta el ítem activo según la ruta actual | 🟡 | Ítem activo con color/fondo diferente |
| 7.6.2 | La navegación entre páginas funciona sin recargar toda la app | 🟠 | Transición de página visible, sin recarga completa |
| 7.6.3 | El botón "Atrás" del navegador funciona correctamente | 🟡 | Navega a la página anterior sin error |
| 7.6.4 | La vista de curso tiene pestañas funcionales (Inicio, Actividades, Anuncios, Calificaciones, Participantes, Recursos) | 🟠 | Cada pestaña carga su contenido sin error |

---

### 7.7 Formularios

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 7.7.1 | Al editar un recurso, el formulario se pre-llena con los datos actuales | 🔴 | Campos del formulario con valores existentes al abrir edición |
| 7.7.2 | Cancelar una edición no persiste los cambios | 🔴 | Datos originales intactos tras cancelar |
| 7.7.3 | Los inputs `type="date"` tienen cursor pointer y funcionan en todos los navegadores (Chrome, Firefox, Edge) | 🟡 | Selector de fecha nativo visible y funcional |
| 7.7.4 | Los campos `required` muestran indicación visual de obligatorio | 🟡 | Asterisco o etiqueta |

---

### 7.8 Accesibilidad básica

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 7.8.1 | Todos los botones de solo ícono tienen `aria-label` descriptivo | 🟠 | Inspeccionar: `<button aria-label="Editar Juan Pérez">` |
| 7.8.2 | Los inputs de búsqueda tienen `aria-label` | 🟠 | `<input aria-label="Buscar estudiante">` o similar |
| 7.8.3 | Los modales tienen `role="dialog"` y `aria-modal="true"` | 🟠 | Inspeccionar el DOM del modal |
| 7.8.4 | Los modales tienen `aria-labelledby` apuntando al título | 🟠 | `aria-labelledby` con el id del h2 del modal |
| 7.8.5 | Los errores dinámicos tienen `role="alert"` | 🟠 | `<div role="alert">` en el componente de error |
| 7.8.6 | Los labels de formularios están vinculados a sus inputs con `for`/`id` | 🟠 | `<label for="period-name">` + `<input id="period-name">` |
| 7.8.7 | La navegación con Tab sigue un orden lógico | 🟡 | Recorrer el formulario con Tab en orden visual |

---

### 7.9 Responsive (comprobación básica)

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 7.9.1 | En 375px (móvil): el sidebar se colapsa o adapta correctamente | 🟡 | Sin overflow horizontal. Sidebar adaptado. |
| 7.9.2 | En 768px (tablet): las tablas son scrollables horizontalmente si no caben | 🟡 | Sin corte de contenido |
| 7.9.3 | En 1440px (escritorio): los paneles usan el espacio disponible | 🟡 | Sin elementos excesivamente pequeños o grandes |
| 7.9.4 | El formulario de crear materia es usable en móvil | 🟡 | Campos apilados verticalmente |

---

### 7.10 Modo oscuro / claro

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 7.10.1 | El toggle de tema cambia entre modo claro y oscuro | 🟡 | Cambio visual inmediato |
| 7.10.2 | En modo claro, el contraste de texto es suficiente (≥ 4.5:1) | 🟡 | Texto legible sobre fondo claro |
| 7.10.3 | En modo oscuro, los bordes y fondos de tarjetas son visibles | 🟡 | Sin tarjetas invisibles sobre fondo oscuro |

---

## 8. Seguridad

---

### 8.1 Cabeceras de seguridad

**Herramienta:** `curl -I http://localhost:5000/api/auth/login -X POST -H "Content-Type: application/json" -d '{"email":"x","password":"x"}'`

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 8.1.1 | Cabecera `X-Content-Type-Options: nosniff` presente | 🟠 | En todas las respuestas |
| 8.1.2 | Cabecera `X-Frame-Options: DENY` presente | 🟠 | En todas las respuestas |
| 8.1.3 | Cabecera `Referrer-Policy: no-referrer` presente | 🟠 | En todas las respuestas |
| 8.1.4 | Las cabeceras están presentes incluso en respuestas de error (401, 404, 500) | 🟠 | Verificar con endpoint inválido |

---

### 8.2 Respuestas 401 y 403

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 8.2.1 | Request sin token a endpoint protegido | 🔴 | 401 `{"statusCode":401,"message":"Token JWT requerido o inválido."}` |
| 8.2.2 | Request con token manipulado (cambiar 1 carácter) | 🔴 | 401 igual que 8.2.1 |
| 8.2.3 | Token de un rol sin permiso para el endpoint | 🔴 | 403 `{"statusCode":403,"message":"No tienes permisos para acceder a este recurso."}` |
| 8.2.4 | La respuesta 401/403 NO incluye información sobre el usuario ni detalles de implementación | 🟠 | Solo `statusCode` y `message` en el JSON |

---

### 8.3 CORS

| # | Caso de prueba | Prioridad | Resultado esperado |
|---|---|---|---|
| 8.3.1 | Request desde origen permitido (localhost:3000) incluye `Access-Control-Allow-Origin` | 🟠 | Cabecera `Access-Control-Allow-Origin: http://localhost:3000` |
| 8.3.2 | Request desde origen NO permitido (ej: evil-domain.com) → sin cabecera CORS | 🟠 | Ausencia de `Access-Control-Allow-Origin` |
| 8.3.3 | Preflight OPTIONS en endpoint protegido responde correctamente | 🟡 | `OPTIONS` → 200 o 204 con cabeceras CORS |

---

### 8.4 Request IDs visibles en errores

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 8.4.1 | Forzar una excepción no manejada → la respuesta 500 incluye `X-Request-Id` en las cabeceras | 🟡 | Cabecera `X-Request-Id: <guid>` en la respuesta |
| 8.4.2 | El cuerpo del error 500 incluye el `requestId` para correlación | 🟡 | `{"statusCode":500,"message":"Ocurrió un error interno del servidor.","requestId":"..."}` |

---

### 8.5 Health check

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 8.5.1 | `GET /health` responde sin necesitar autenticación | 🟠 | HTTP 200 sin token |
| 8.5.2 | Respuesta incluye estado de la base de datos | 🟠 | `{"status":"Healthy","entries":{"database":{"status":"Healthy"}}}` |
| 8.5.3 | Con la BD apagada, `GET /health` responde 503 Unhealthy | 🟠 | HTTP 503 con status "Unhealthy" |

---

## 9. Testing / DevOps

---

### 9.1 Pruebas unitarias

| # | Comando | Prioridad | Resultado esperado |
|---|---|---|---|
| 9.1.1 | `dotnet test tests/UnitTests --configuration Release` | 🔴 | `Passed! Failed: 0` |
| 9.1.2 | Tiempo de ejecución < 30 segundos | 🟡 | Sin Docker ni bases de datos externas |

---

### 9.2 Pruebas de integración

**Precondición:** Docker Desktop en ejecución.

| # | Comando | Prioridad | Resultado esperado |
|---|---|---|---|
| 9.2.1 | `dotnet test tests/IntegrationTests --configuration Release` | 🔴 | `Passed! Failed: 0` |
| 9.2.2 | Testcontainers levanta y destruye el contenedor de SQL Server automáticamente | 🟠 | No quedan contenedores residuales tras la ejecución |

---

### 9.3 Todas las pruebas

| # | Comando | Prioridad | Resultado esperado |
|---|---|---|---|
| 9.3.1 | `dotnet test --configuration Release` | 🔴 | `Passed! Failed: 0` |

---

### 9.4 Build y lint del frontend

| # | Comando | Prioridad | Resultado esperado |
|---|---|---|---|
| 9.4.1 | `cd sistema-evaluacion-academica && npm run lint` | 🔴 | 0 errores (las 2 advertencias pre-existentes de `react-hooks/incompatible-library` son conocidas y aceptadas) |
| 9.4.2 | `cd sistema-evaluacion-academica && npm run build` | 🔴 | Build exitoso. Sin errores de TypeScript ni de Next.js |
| 9.4.3 | `dotnet build --configuration Release` | 🔴 | `Build succeeded. 0 Warning(s), 0 Error(s)` |

---

### 9.5 Docker en CI

| # | Verificación | Prioridad | Resultado esperado |
|---|---|---|---|
| 9.5.1 | `docker compose up -d && docker compose ps` → contenedor `healthy` | 🔴 | Status: `healthy` en columna STATUS |
| 9.5.2 | `docker compose down -v` elimina el volumen y datos | 🟠 | Sin volúmenes residuales tras `down -v` |
| 9.5.3 | Nuevo `docker compose up -d` + arranque de backend re-siembra los datos | 🟠 | Seeder ejecuta correctamente con DB vacía |

---

## 10. Checklist Final de Publicación (Pre-Deploy)

### 10.1 Configuración de entorno

| # | Verificación | Prioridad |
|---|---|---|
| 10.1.1 | `JwtSettings:SecretKey` en producción es diferente al de development y tiene ≥ 32 caracteres | 🔴 |
| 10.1.2 | `JwtSettings:SecretKey` en producción NO es ningún placeholder conocido | 🔴 |
| 10.1.3 | `SA_PASSWORD` en `.env` es segura y diferente al default `Dev@Password_2024!` | 🔴 |
| 10.1.4 | `Cors:AllowedOrigins` contiene solo los dominios de producción | 🔴 |
| 10.1.5 | `RateLimit:LoginPermitLimit` es 5 (o valor apropiado) en producción | 🟠 |
| 10.1.6 | `JwtSettings:ExpirationMinutes` en producción tiene valor apropiado (ej: 480 = 8h) | 🟠 |
| 10.1.7 | El archivo `.env` no está commiteado en el repositorio | 🔴 |
| 10.1.8 | `.env.example` tiene todos los campos necesarios documentados | 🟠 |

---

### 10.2 Backend

| # | Verificación | Prioridad |
|---|---|---|
| 10.2.1 | `dotnet build --configuration Release` sin errores ni warnings | 🔴 |
| 10.2.2 | `dotnet test --configuration Release` → todas las pruebas pasan | 🔴 |
| 10.2.3 | Swagger UI deshabilitado en Production (solo en Development) | 🟠 |
| 10.2.4 | Logs configurados en nivel apropiado para producción (no Debug) | 🟠 |
| 10.2.5 | HTTPS enforcement configurado | 🔴 |
| 10.2.6 | `GET /health` responde 200 con DB disponible | 🔴 |

---

### 10.3 Frontend

| # | Verificación | Prioridad |
|---|---|---|
| 10.3.1 | `npm run build` exitoso sin errores | 🔴 |
| 10.3.2 | `npm run lint` sin errores (advertencias conocidas documentadas) | 🔴 |
| 10.3.3 | `NEXT_PUBLIC_API_URL` apunta al backend de producción | 🔴 |
| 10.3.4 | Archivos `.env.local` no están commiteados | 🔴 |
| 10.3.5 | La página de login carga correctamente sin conexión al API (estado de error amigable) | 🟠 |

---

### 10.4 Base de datos

| # | Verificación | Prioridad |
|---|---|---|
| 10.4.1 | Todas las migraciones EF Core aplicadas en el entorno destino | 🔴 |
| 10.4.2 | Índice único en `(StudentId, SectionId)` existe en tabla `SectionEnrollments` | 🔴 |
| 10.4.3 | Los backups de la base de datos están configurados | 🟠 |
| 10.4.4 | La cadena de conexión usa `TrustServerCertificate=False` en producción (o certificado válido) | 🟠 |

---

### 10.5 Seguridad final

| # | Verificación | Prioridad |
|---|---|---|
| 10.5.1 | Cabeceras `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` presentes | 🟠 |
| 10.5.2 | Rate limiting de login activo con valor apropiado | 🟠 |
| 10.5.3 | CORS configurado con orígenes específicos (no `AllowAnyOrigin`) en producción | 🔴 |
| 10.5.4 | HTTPS forzado. Sin tráfico HTTP plano en producción. | 🔴 |
| 10.5.5 | El endpoint `/health` no devuelve información sensible de la BD | 🟠 |

---

## Áreas de Mayor Riesgo

### 🔴 Riesgo Crítico

1. **Lógica de inscripción (13 validaciones)** — Es la regla de negocio más compleja del sistema. Un error aquí puede resultar en inscripciones inválidas, duplicadas o datos corruptos. Verificar especialmente el flujo transaccional bajo concurrencia.

2. **Autorización por roles** — Cualquier agujero permite que Estudiantes accedan a datos de otros o realicen operaciones de Admin/Profesor. Probar explícitamente los 403 con cada rol.

3. **Prerequisitos circulares** — Un ciclo en la tabla de prerequisitos podría causar loops infinitos en la carga del catálogo. Probar la detección de ciclos explícitamente.

4. **JWT y sesión** — El token en `localStorage` es el único mecanismo de auth. Un token válido robado otorga acceso completo. Verificar que la expiración y el flujo de 401 funcionan correctamente.

5. **Soft delete** — Las eliminaciones no son físicas. Verificar que los filtros EF Core (`IsActive`) se aplican correctamente en todos los endpoints.

### 🟠 Riesgo Alto

6. **Período activo y control de inscripciones** — Solo un período debe estar abierto a la vez. Abrir un nuevo período sin cerrar el anterior podría causar inscripciones en períodos incorrectos.

7. **Materia soft-deleted con secciones activas** — El caso de `section.Subject is null` (materia eliminada con sección existente) debe manejar correctamente el enrollment y el catálogo.

8. **Race condition en capacidad de sección** — El re-check bajo transacción serializable es la única protección contra sobre-inscripción. Probar explícitamente con 2 requests simultáneos a la última plaza.

9. **Ownership de submissions/anuncios/actividades** — Un profesor no debe poder editar o calificar contenido de las secciones de otro profesor.

---

## Smoke Checklist — 5 Minutos Antes del Deploy

Verificar en orden estricto. Si cualquier punto falla, **no hacer deploy.**

```
SMOKE TEST — Sistema de Evaluación Académica
Fecha: _______________  Tester: _______________  Entorno: _______________

[ ] 1. GET /health → HTTP 200 "Healthy"
[ ] 2. Login admin@academia.com / Admin123! → 200 + token JWT
[ ] 3. Login con contraseña incorrecta → 401 (no 500)
[ ] 4. GET /api/students (con token Admin) → 200 + lista de estudiantes
[ ] 5. GET /api/academic-periods (con token Admin) → 200 + período activo
[ ] 6. GET /api/students (sin token) → 401
[ ] 7. Login juan.perez@academia.com / Estudiante123! → 200
[ ] 8. GET /api/enrollments/catalog/me (con token Juan) → 200 + catálogo
[ ] 9. GET /api/students (con token Juan) → 403
[ ] 10. npm run build (frontend) → 0 errores
[ ] 11. dotnet test tests/UnitTests → Failed: 0
[ ] 12. Cabecera X-Frame-Options: DENY en respuesta de /api/auth/login

RESULTADO: [ ] APTO PARA DEPLOY   [ ] NO APTO — Ver ítem(s): _______________
```

---

*Documento generado: 2026-05-19. Actualizar tras cada release con los resultados obtenidos.*
