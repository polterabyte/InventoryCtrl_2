# Use the official .NET 8.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Inventory.Web.Client/Inventory.Web.Client.csproj", "src/Inventory.Web.Client/"]
COPY ["src/Inventory.Shared/Inventory.Shared.csproj", "src/Inventory.Shared/"]
COPY ["src/Inventory.UI/Inventory.UI.csproj", "src/Inventory.UI/"]

# Restore dependencies
RUN dotnet restore "src/Inventory.Web.Client/Inventory.Web.Client.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Inventory.Web.Client"
RUN dotnet build "Inventory.Web.Client.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Inventory.Web.Client.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use nginx as base image for serving static files
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Remove default nginx static assets
RUN rm -rf ./*

# Copy published Blazor WebAssembly files
COPY --from=publish /app/publish/wwwroot .

# Copy nginx configuration
COPY src/Inventory.Web.Client/nginx.conf /etc/nginx/nginx.conf

# Expose port 80
EXPOSE 80

# Domain configuration
ENV DOMAIN=warehouse.cuby
ENV STAGING_DOMAIN=staging.warehouse.cuby
ENV TEST_DOMAIN=test.warehouse.cuby

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
