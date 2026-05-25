# Deployment Guide

Stack recomendado para portfolio público:

| Componente | Servicio | Tier gratuito |
|------------|---------|---------------|
| Frontend | [Vercel](https://vercel.com) | Sí |
| Backend | [Railway](https://railway.app) | $5/mes de crédito |
| Base de datos | SQL Server en Railway | Incluido en proyecto Railway |

---

## Variables de entorno requeridas

### Backend (Railway / cualquier host)

| Variable | Ejemplo | Notas |
|----------|---------|-------|
| `ConnectionStrings__DefaultConnection` | `Server=containers-xxx.railway.internal,1433;Database=SistemaEvaluacion;User Id=sa;Password=XXX;TrustServerCertificate=True;` | SQL auth — no Windows auth |
| `JwtSettings__SecretKey` | `openssl rand -base64 48` | Mínimo 32 chars — obligatorio |
| `Cors__AllowedOrigins__0` | `https://tu-app.vercel.app` | URL exacta de Vercel |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Desactiva Swagger, activa CSP estricta |
| `ASPNETCORE_URLS` | `http://+:8080` | Debe coincidir con el puerto expuesto |

### Frontend (Vercel)

| Variable | Valor |
|----------|-------|
| `NEXT_PUBLIC_API_URL` | `https://tu-backend.railway.app` |

---

## Opción A — Railway (backend + BD) + Vercel (frontend)

### 1. Base de datos — SQL Server en Railway

1. Crear proyecto nuevo en Railway
2. **New Service → Docker Image** → imagen: `mcr.microsoft.com/mssql/server:2022-latest`
3. Variables de entorno del contenedor SQL Server:
   ```
   ACCEPT_EULA=Y
   MSSQL_SA_PASSWORD=<contraseña-segura>
   ```
4. Railway asigna una URL interna. Anotar el host y puerto (visibles en la pestaña **Connect** del servicio).

### 2. Backend — contenedor desde GitHub

1. En el mismo proyecto Railway, **New Service → GitHub Repo**
2. Seleccionar el repositorio; Railway detecta el `Dockerfile` en la raíz automáticamente
3. Variables de entorno del backend (pestaña **Variables**):
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=http://+:8080
   ConnectionStrings__DefaultConnection=Server=<host-interno-railway>,1433;Database=SistemaEvaluacion;User Id=sa;Password=<contraseña>;TrustServerCertificate=True;
   JwtSettings__SecretKey=<clave-generada-con-openssl>
   Cors__AllowedOrigins__0=https://<tu-app>.vercel.app
   ```
4. En Railway: **Settings → Networking → Public Networking** → exponer el servicio en el puerto `8080`
5. Anotar la URL pública generada (ej. `https://sistema-evaluacion-production.railway.app`)

> El backend aplica migraciones y siembra datos demo automáticamente en el primer arranque.
> Verificar en los logs que aparezca el mensaje de migración exitosa.

### 3. Frontend — Vercel

1. Importar el repositorio en [vercel.com/new](https://vercel.com/new)
2. **Root Directory**: `sistema-evaluacion-academica`
3. **Framework Preset**: Next.js (detectado automáticamente)
4. Variables de entorno en Vercel:
   ```
   NEXT_PUBLIC_API_URL=https://<tu-backend>.railway.app
   ```
5. Deploy

### 4. Verificar

```bash
# Health check del backend
curl https://<tu-backend>.railway.app/health
# Respuesta esperada: {"status":"Healthy",...}

# Login con usuario demo
curl -X POST https://<tu-backend>.railway.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@academia.com","password":"Admin123!"}'
```

---

## Opción B — Azure (más profesional para portfolio)

| Componente | Servicio Azure |
|------------|----------------|
| Backend | Azure App Service (Free F1 o Basic B1) |
| Base de datos | Azure SQL Database (Free tier: 32 GB) |
| Frontend | Vercel (igual que Opción A) |

### Pasos resumidos

```bash
# 1. Crear grupo de recursos
az group create --name rg-academia --location eastus

# 2. Crear Azure SQL Database
az sql server create --name sql-academia --resource-group rg-academia \
  --location eastus --admin-user sqladmin --admin-password <contraseña>
az sql db create --resource-group rg-academia --server sql-academia \
  --name SistemaEvaluacion --service-objective Free

# 3. Crear App Service y desplegar desde GitHub
az webapp create --resource-group rg-academia --plan plan-academia \
  --name api-academia --runtime "DOTNETCORE:8.0"

# 4. Configurar variables de entorno en App Service
az webapp config appsettings set --resource-group rg-academia --name api-academia --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  "ConnectionStrings__DefaultConnection=<connection-string-azure-sql>" \
  JwtSettings__SecretKey=<clave> \
  Cors__AllowedOrigins__0=https://<tu-app>.vercel.app
```

La connection string de Azure SQL usa formato:
```
Server=tcp:sql-academia.database.windows.net,1433;Database=SistemaEvaluacion;User Id=sqladmin;Password=<contraseña>;Encrypt=True;
```

---

## Checklist de primer deployment

- [ ] `JwtSettings__SecretKey` configurado con valor real (≥ 32 chars, no placeholder)
- [ ] `ConnectionStrings__DefaultConnection` apunta al servidor de producción con SQL auth
- [ ] `Cors__AllowedOrigins__0` contiene la URL exacta del frontend en Vercel
- [ ] `ASPNETCORE_ENVIRONMENT=Production` (desactiva Swagger, activa logs reducidos)
- [ ] `NEXT_PUBLIC_API_URL` en Vercel apunta al backend de producción (no localhost)
- [ ] Health check devuelve `{"status":"Healthy"}` antes de probar login
- [ ] Login con `admin@academia.com` / `Admin123!` funciona desde el frontend

---

## Notas de deployment

**Migraciones:** el backend aplica migraciones automáticamente en cada arranque vía `MigrateAsync()`. Es idempotente — si las migraciones ya están aplicadas, no hace nada. No es necesario correr `dotnet ef database update` manualmente.

**Datos demo:** el seeder también corre en cada arranque. Es idempotente — no duplica registros existentes. Los 6 usuarios demo (`admin@academia.com`, `prof.garcia@academia.com`, etc.) estarán disponibles desde el primer arranque.

**Swagger:** desactivado en `Production`. Solo accesible en `Development`. No requiere configuración adicional.

**HTTPS:** el backend escucha en HTTP internamente. TLS termina en el reverse proxy del proveedor (Railway/Render/Azure). `UseForwardedHeaders()` está configurado para que `UseHttpsRedirection()` funcione correctamente con el header `X-Forwarded-Proto`.

**Rate limiting:** 5 solicitudes de login por minuto por IP en producción. Ajustable con `RateLimit__LoginPermitLimit`.

---

## Riesgos de deployment restantes

| Riesgo | Severidad | Notas |
|--------|-----------|-------|
| JWT en localStorage | Media | Documentado — aceptable para portfolio sin datos reales |
| Sin refresh tokens | Baja | Sesión expira en 8 h, redirige a login |
| SQL Server container en Railway sin backups | Media | Para portfolio: aceptable. Producción real: usar managed DB |
| Sin HTTPS forzado a nivel de app | Baja | Railway/Vercel fuerzan HTTPS en el proxy |
| Sin paginación en endpoints de lista | Baja | Funcional a escala demo |
