FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaurar dependencias primero (capa cacheada si los .csproj no cambian)
COPY src/API/SistemaEvaluacionAcademica.API.csproj             src/API/
COPY src/Application/SistemaEvaluacionAcademica.Application.csproj src/Application/
COPY src/Domain/SistemaEvaluacionAcademica.Domain.csproj       src/Domain/
COPY src/Infrastructure/SistemaEvaluacionAcademica.Infrastructure.csproj src/Infrastructure/
RUN dotnet restore src/API/SistemaEvaluacionAcademica.API.csproj

COPY src/ src/
RUN dotnet publish src/API/SistemaEvaluacionAcademica.API.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway / Render inyectan PORT automáticamente.
# Sobreescribir con ASPNETCORE_URLS=http://+:$PORT en el dashboard si el puerto difiere.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SistemaEvaluacionAcademica.API.dll"]
