# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching.
# Directory.Packages.props and Directory.Build.targets are required: package
# versions are managed centrally, so restore fails without the first, and the
# second carries the per-project-type build settings.
COPY Directory.Build.props Directory.Build.targets Directory.Packages.props ./
COPY Aelena.FileApi.sln ./
COPY src/Aelena.FileApi.Core/Aelena.FileApi.Core.csproj src/Aelena.FileApi.Core/
COPY src/Aelena.FileApi.Api/Aelena.FileApi.Api.csproj src/Aelena.FileApi.Api/

RUN dotnet restore src/Aelena.FileApi.Api/Aelena.FileApi.Api.csproj

# Copy everything and publish.
# -f is required: the projects multi-target net10.0 and net11.0, and this image
# ships the LTS runtime.
COPY src/ src/
COPY prompts/ prompts/
RUN dotnet publish src/Aelena.FileApi.Api/Aelena.FileApi.Api.csproj \
    -f net10.0 -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim AS runtime

# System dependencies for document processing
RUN apt-get update && apt-get install -y --no-install-recommends \
    tesseract-ocr \
    tesseract-ocr-eng \
    tesseract-ocr-spa \
    libreoffice-core \
    libreoffice-writer \
    libreoffice-calc \
    libreoffice-impress \
    ghostscript \
    libgdiplus \
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
    CMD curl -f http://localhost:9401/health || exit 1

ENTRYPOINT ["dotnet", "Aelena.FileApi.Api.dll"]
