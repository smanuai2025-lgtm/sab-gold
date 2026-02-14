# ═══════════════════════════════════════════════════════════════════
# SabeekaGold Backend - Production Dockerfile
# ═══════════════════════════════════════════════════════════════════

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Ensure NuGet packages go to a Linux-friendly folder inside the container
ENV NUGET_PACKAGES=/root/.nuget/packages

# Copy solution and project files
COPY *.sln ./
COPY MrGoldenBader.API/*.csproj ./MrGoldenBader.API/
COPY MrGoldenBader.Application/*.csproj ./MrGoldenBader.Application/
COPY MrGoldenBader.Domain/*.csproj ./MrGoldenBader.Domain/
# Copy infra project csproj
COPY MrGoldenBader.Infrastructure/*.csproj ./MrGoldenBader.Infrastructure/

# Copy all source code
COPY . .

# Remove any existing build artifacts that may reference Windows-specific paths
RUN find /src -type d -name obj -prune -exec rm -rf {} + || true
RUN find /src -type d -name bin -prune -exec rm -rf {} + || true

# Restore dependencies (after removing repo obj/bin to avoid Windows-specific references)
RUN dotnet restore

# Build and publish
WORKDIR /src/MrGoldenBader.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install security updates
RUN apt-get update && apt-get upgrade -y && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Copy published files
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /var/log/sabeekagold && chown -R appuser:appgroup /var/log/sabeekagold

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Set environment
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

# Run application
ENTRYPOINT ["dotnet", "MrGoldenBader.API.dll"]
