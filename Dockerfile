# syntax=docker/dockerfile:1
# Multi-stage build: SDK image compiles/publishes, slim ASP.NET runtime image serves.
# Built directly on the target host (works on amd64 and arm64/Raspberry Pi alike).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first with only the project/solution metadata so the NuGet layer is
# cached as long as no dependency changes (Central Package Management: the
# package versions live in Directory.Packages.props).
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Dashboard.Domain/Dashboard.Domain.csproj src/Dashboard.Domain/
COPY src/Dashboard.Infrastructure/Dashboard.Infrastructure.csproj src/Dashboard.Infrastructure/
COPY src/Dashboard.Web/Dashboard.Web.csproj src/Dashboard.Web/
RUN dotnet restore src/Dashboard.Web/Dashboard.Web.csproj

COPY src/ src/
RUN dotnet publish src/Dashboard.Web/Dashboard.Web.csproj \
    --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Writable dirs for the offline-proxy caches and Serilog file logs; owned by the
# unprivileged app user so the container does not run as root. Named volumes
# mounted here inherit this ownership on first use.
RUN mkdir -p tile-cache crest-cache logs \
    && chown -R $APP_UID:$APP_UID tile-cache crest-cache logs
USER $APP_UID

# The aspnet base image listens on 8080 (ASPNETCORE_HTTP_PORTS); map it to the
# LAN-facing port in docker-compose.
EXPOSE 8080
ENTRYPOINT ["dotnet", "Dashboard.Web.dll"]
