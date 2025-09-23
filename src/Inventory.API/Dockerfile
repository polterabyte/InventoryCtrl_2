# Use the official .NET 8.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 8.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Inventory.API/Inventory.API.csproj", "src/Inventory.API/"]
COPY ["src/Inventory.Shared/Inventory.Shared.csproj", "src/Inventory.Shared/"]
COPY ["src/Inventory.Web.Client/Inventory.Web.Client.csproj", "src/Inventory.Web.Client/"]

# Restore dependencies
RUN dotnet restore "src/Inventory.API/Inventory.API.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Inventory.API"
RUN dotnet build "Inventory.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Inventory.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Create final runtime image
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_HTTPS_PORT=7000

# Domain configuration
ENV DOMAIN=warehouse.cuby
ENV STAGING_DOMAIN=staging.warehouse.cuby
ENV TEST_DOMAIN=test.warehouse.cuby

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/api/health || exit 1

ENTRYPOINT ["dotnet", "Inventory.API.dll"]
