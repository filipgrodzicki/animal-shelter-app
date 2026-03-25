# ===========================================
# Stage 1: Base runtime image
# ===========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

# Install curl for health checks
RUN apk add --no-cache curl

# Create non-root user for security
RUN adduser -D -u 1000 appuser
USER appuser

# ===========================================
# Stage 2: Build
# ===========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only project files first (better layer caching)
COPY ["src/ShelterApp.Api/ShelterApp.Api.csproj", "src/ShelterApp.Api/"]
COPY ["src/ShelterApp.Domain/ShelterApp.Domain.csproj", "src/ShelterApp.Domain/"]
COPY ["src/ShelterApp.Infrastructure/ShelterApp.Infrastructure.csproj", "src/ShelterApp.Infrastructure/"]

# Restore dependencies (cached if .csproj files don't change)
RUN dotnet restore "src/ShelterApp.Api/ShelterApp.Api.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/ShelterApp.Api"
RUN dotnet build "ShelterApp.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# ===========================================
# Stage 3: Publish
# ===========================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ShelterApp.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false

# ===========================================
# Stage 4: Final runtime image
# ===========================================
FROM base AS final
WORKDIR /app

# Copy published app from publish stage
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ShelterApp.Api.dll"]
