# ── Build stage ───────────────────────────────────────────────────────────────
# Ubuntu Noble, matching the runtime stage. .NET 10 ships no Debian-based GA
# image — bookworm is gone and trixie exists only as preview tags — so "noble"
# is the apt-based family now, and the LibreOffice/Tesseract packages below need
# apt.
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# The repository multi-targets net10.0 and net11.0, and this image carries only
# the .NET 10 SDK, which cannot restore a net11.0 target at all. Restricting the
# frameworks here is the same escape hatch the README documents for contributors
# without the .NET 11 SDK.
ARG TARGET_FRAMEWORK=net10.0

# Copy project files first for layer caching.
# Directory.Packages.props and Directory.Build.targets are required: package
# versions are managed centrally, so restore fails without the first, and the
# second carries the per-project-type build settings.
COPY Directory.Build.props Directory.Build.targets Directory.Packages.props ./
COPY Aelena.FileApi.sln ./
COPY src/Aelena.FileApi.Core/Aelena.FileApi.Core.csproj src/Aelena.FileApi.Core/
COPY src/Aelena.FileApi.Core.Pdf/Aelena.FileApi.Core.Pdf.csproj src/Aelena.FileApi.Core.Pdf/
COPY src/Aelena.FileApi.Api/Aelena.FileApi.Api.csproj src/Aelena.FileApi.Api/

RUN dotnet restore src/Aelena.FileApi.Api/Aelena.FileApi.Api.csproj \
    -p:TargetFrameworks=$TARGET_FRAMEWORK

COPY src/ src/
COPY prompts/ prompts/

RUN dotnet publish src/Aelena.FileApi.Api/Aelena.FileApi.Api.csproj \
    -f $TARGET_FRAMEWORK -c Release -o /app/publish --no-restore \
    -p:TargetFrameworks=$TARGET_FRAMEWORK

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime

# System dependencies for document processing.
#
# curl is here for the HEALTHCHECK below: the aspnet images do not ship it, so
# the check invoked a binary that did not exist and every container reported
# itself unhealthy regardless of whether the app was serving.
#
# libgdiplus was dropped: it backs System.Drawing, which this project does not
# reference anywhere — imaging goes through ImageSharp, which is fully managed.
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    tesseract-ocr \
    tesseract-ocr-eng \
    tesseract-ocr-spa \
    libreoffice-core \
    libreoffice-writer \
    libreoffice-calc \
    libreoffice-impress \
    ghostscript \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/prompts/ prompts/

# Create data and logs directories
RUN mkdir -p /app/data /app/logs

ENV ASPNETCORE_URLS=http://+:9401
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 9401

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD curl -fsS http://localhost:9401/health || exit 1

ENTRYPOINT ["dotnet", "Aelena.FileApi.Api.dll"]
