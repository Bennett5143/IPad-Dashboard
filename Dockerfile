# syntax=docker/dockerfile:1
# Multi-stage build: SDK image compiles/publishes, slim ASP.NET runtime image serves.
# Multi-arch aware: in CI (buildx) the build stage runs natively on the runner
# ($BUILDPLATFORM) and cross-compiles for $TARGETARCH — no QEMU-emulated compiler.
# In a plain single-arch `docker build` (Pi fallback, dev Mac) BuildKit sets both
# to the host values, so nothing changes there.

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restore first with only the project/solution metadata so the NuGet layer is
# cached as long as no dependency changes (Central Package Management: the
# package versions live in Directory.Packages.props).
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Dashboard.Domain/Dashboard.Domain.csproj src/Dashboard.Domain/
COPY src/Dashboard.Infrastructure/Dashboard.Infrastructure.csproj src/Dashboard.Infrastructure/
COPY src/Dashboard.Web/Dashboard.Web.csproj src/Dashboard.Web/
RUN dotnet restore src/Dashboard.Web/Dashboard.Web.csproj -a $TARGETARCH

COPY src/ src/
# Kein --no-restore: Das Web-SDK fügt Microsoft.AspNetCore.App.Internal.Assets
# (liefert _framework/blazor.web.js) nur hinzu, wenn beim Restore .razor-Dateien
# existieren — im Metadaten-only-Restore oben fehlen sie. Der zweite Restore ist
# dank des gewärmten NuGet-Caches schnell; der Layer oben bleibt der Cache-Wärmer.
RUN dotnet publish src/Dashboard.Web/Dashboard.Web.csproj \
    --configuration Release --output /app/publish -a $TARGETARCH

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Writable dirs for the offline-proxy caches, Serilog file logs and the Data
# Protection key ring (antiforgery + Blazor circuit descriptors survive container
# recreation); owned by the unprivileged app user so the container does not run
# as root. Named volumes mounted here inherit this ownership on first use.
RUN mkdir -p tile-cache crest-cache logs /home/app/.aspnet/DataProtection-Keys \
    && chown -R $APP_UID:$APP_UID tile-cache crest-cache logs /home/app/.aspnet
USER $APP_UID

# The aspnet base image listens on 8080 (ASPNETCORE_HTTP_PORTS); map it to the
# LAN-facing port in docker-compose.
EXPOSE 8080
ENTRYPOINT ["dotnet", "Dashboard.Web.dll"]
