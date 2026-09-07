# syntax=docker/dockerfile:1
# Multi-stage build: SDK image compiles/publishes, slim ASP.NET runtime image serves.
# Multi-arch aware: in CI (buildx) the build stage runs natively on the runner
# ($BUILDPLATFORM) and cross-compiles for $TARGETARCH — no QEMU-emulated compiler.
# In a plain single-arch `docker build` (Pi fallback, dev Mac) BuildKit sets both
# to the host values, so nothing changes there.

# Base images are digest-pinned (supply-chain integrity; Dependabot bumps the
# digests). The digest is the manifest-list digest, valid for amd64 and arm64.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0@sha256:4beef5b8919dcaa2dc924233bd069257e883cc7a061e09088a97d152d6a48510 AS build
ARG TARGETARCH
WORKDIR /src

# Restore first with only the project/solution metadata so the NuGet layer is
# cached as long as no dependency changes (Central Package Management: the
# package versions live in Directory.Packages.props).
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Dashboard.Domain/Dashboard.Domain.csproj src/Dashboard.Domain/
COPY src/Dashboard.Infrastructure/Dashboard.Infrastructure.csproj src/Dashboard.Infrastructure/
COPY src/Dashboard.Web/Dashboard.Web.csproj src/Dashboard.Web/
# Deliberately NOT --locked-mode: this metadata-only restore lacks the .razor
# files, so the Web SDK omits Microsoft.AspNetCore.App.Internal.Assets here and
# the graph can never match the committed packages.lock.json (NU1004). This
# stage only warms the NuGet layer cache; the CI restores are locked.
RUN dotnet restore src/Dashboard.Web/Dashboard.Web.csproj -a $TARGETARCH -p:RestorePackagesWithLockFile=false

COPY src/ src/
# Kein --no-restore: Das Web-SDK fügt Microsoft.AspNetCore.App.Internal.Assets
# (liefert _framework/blazor.web.js) nur hinzu, wenn beim Restore .razor-Dateien
# existieren — im Metadaten-only-Restore oben fehlen sie. Der zweite Restore ist
# dank des gewärmten NuGet-Caches schnell; der Layer oben bleibt der Cache-Wärmer.
# RestoreLockedMode: with the full sources present the graph matches the
# committed lock files (incl. the RID sections), so this restore — the one
# that feeds the shipped image — is enforced against them.
RUN dotnet publish src/Dashboard.Web/Dashboard.Web.csproj \
    --configuration Release --output /app/publish -a $TARGETARCH \
    -p:RestoreLockedMode=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:011bb5f30180717b1c8b65822ff2c99bcb96bc65af0164589751b83c7b4949f7 AS runtime
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
