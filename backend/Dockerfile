# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Build configuration (Release for production, Staging for staging/test)
ARG BUILD_CONFIGURATION=Release

# Copy solution and project files
COPY ExpenseFlow.sln ./
COPY src/ExpenseFlow.Api/ExpenseFlow.Api.csproj src/ExpenseFlow.Api/
COPY src/ExpenseFlow.Core/ExpenseFlow.Core.csproj src/ExpenseFlow.Core/
COPY src/ExpenseFlow.Infrastructure/ExpenseFlow.Infrastructure.csproj src/ExpenseFlow.Infrastructure/
COPY src/ExpenseFlow.Shared/ExpenseFlow.Shared.csproj src/ExpenseFlow.Shared/
COPY tests/ExpenseFlow.Api.Tests/ExpenseFlow.Api.Tests.csproj tests/ExpenseFlow.Api.Tests/
COPY tests/ExpenseFlow.Core.Tests/ExpenseFlow.Core.Tests.csproj tests/ExpenseFlow.Core.Tests/
COPY tests/ExpenseFlow.Infrastructure.Tests/ExpenseFlow.Infrastructure.Tests.csproj tests/ExpenseFlow.Infrastructure.Tests/
COPY tests/ExpenseFlow.Contracts.Tests/ExpenseFlow.Contracts.Tests.csproj tests/ExpenseFlow.Contracts.Tests/
COPY tests/ExpenseFlow.PropertyTests/ExpenseFlow.PropertyTests.csproj tests/ExpenseFlow.PropertyTests/
COPY tests/ExpenseFlow.Scenarios.Tests/ExpenseFlow.Scenarios.Tests.csproj tests/ExpenseFlow.Scenarios.Tests/
COPY tests/ExpenseFlow.TestCommon/ExpenseFlow.TestCommon.csproj tests/ExpenseFlow.TestCommon/
COPY tests/ExpenseFlow.LoadTests/ExpenseFlow.LoadTests.csproj tests/ExpenseFlow.LoadTests/

# Restore dependencies (only API project - tests use different SDK)
RUN dotnet restore src/ExpenseFlow.Api/ExpenseFlow.Api.csproj

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/ExpenseFlow.Api/ExpenseFlow.Api.csproj -c ${BUILD_CONFIGURATION} -o /app/publish

# Runtime stage - Using Debian for Chromium/PuppeteerSharp support (HTML receipt thumbnails)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

# Install Chromium and dependencies for PuppeteerSharp (HTML-to-image rendering)
# Plus Ghostscript for Magick.NET PDF rendering (receipt thumbnails)
# Plus ImageMagick for thumbnail generation
# Plus wget for health checks
RUN apt-get update && apt-get install -y \
    chromium \
    ghostscript \
    imagemagick \
    fonts-liberation \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgbm1 \
    libnspr4 \
    libnss3 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxkbcommon0 \
    libxrandr2 \
    xdg-utils \
    wget \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/* \
    # Fix ImageMagick security policy to allow PDF processing
    # Default Debian policy blocks PDF for security, but we need it for receipt thumbnails
    && sed -i 's/<policy domain="coder" rights="none" pattern="PDF" \/>/<policy domain="coder" rights="read|write" pattern="PDF" \/>/g' /etc/ImageMagick-6/policy.xml 2>/dev/null || true \
    && sed -i 's/<policy domain="coder" rights="none" pattern="PDF"/<policy domain="coder" rights="read|write" pattern="PDF"/g' /etc/ImageMagick-6/policy.xml 2>/dev/null || true

# Set Chromium path for PuppeteerSharp
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium

# Create non-root user for security
RUN useradd -m -u 1000 appuser

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "ExpenseFlow.Api.dll"]
