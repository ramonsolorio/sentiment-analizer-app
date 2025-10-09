# ========================================
# Multi-stage Dockerfile for .NET 6 Backend
# Target: Linux containers (AMD64/ARM64)
# ========================================

# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy everything else
COPY . .

# Build the application
RUN dotnet build "SentimentAnalyzer.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SentimentAnalyzer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - Linux only
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS final
WORKDIR /app

# Install dependencies for .NET globalization and healthchecks
RUN apk add --no-cache curl icu-libs

# Set environment variable for ICU
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user for security
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser && \
    chown -R appuser:appuser /app

USER appuser

# Expose ports
EXPOSE 80
EXPOSE 443

# Environment variables
ENV ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "SentimentAnalyzer.dll"]