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

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/ExpenseFlow.Api/ExpenseFlow.Api.csproj -c ${BUILD_CONFIGURATION} -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Install ICU libraries for SQL Server client globalization support
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Create non-root user for security
RUN adduser -D -u 1000 appuser

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
