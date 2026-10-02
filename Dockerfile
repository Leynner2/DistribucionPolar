# syntax=docker/dockerfile:1

# ---------- Etapa 1: compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar primero solo con los .csproj para aprovechar la caché de capas.
COPY Core.Domain/Core.Domain.csproj Core.Domain/
COPY Core.Application/Core.Application.csproj Core.Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Presentation.API/Presentation.API.csproj Presentation.API/
RUN dotnet restore Presentation.API/Presentation.API.csproj

COPY Core.Domain/ Core.Domain/
COPY Core.Application/ Core.Application/
COPY Infrastructure/ Infrastructure/
COPY Presentation.API/ Presentation.API/
RUN dotnet publish Presentation.API/Presentation.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Etapa 2: ejecución ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl para el healthcheck de Docker (GET /health).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Usuario sin privilegios incluido en la imagen oficial.
USER $APP_UID
ENTRYPOINT ["dotnet", "Presentation.API.dll"]
