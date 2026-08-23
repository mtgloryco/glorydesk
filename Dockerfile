# syntax=docker/dockerfile:1
# Builds and runs InventoryManagementSystem.Cloud (the Glory Desk Cloud API) for Railway.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so `dotnet restore` is cached unless deps actually change.
COPY Directory.Build.props ./
COPY InventoryManagementSystem.Cloud/InventoryManagementSystem.Cloud.csproj InventoryManagementSystem.Cloud/
RUN dotnet restore InventoryManagementSystem.Cloud/InventoryManagementSystem.Cloud.csproj

COPY InventoryManagementSystem.Cloud/ InventoryManagementSystem.Cloud/
RUN dotnet publish InventoryManagementSystem.Cloud/InventoryManagementSystem.Cloud.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN useradd --create-home appuser
COPY --from=build /app/publish .
RUN mkdir -p /app/backups && chown -R appuser:appuser /app
USER appuser

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Railway injects PORT at runtime; ASPNETCORE_URLS is resolved via shell expansion below.
EXPOSE 8080

ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet InventoryManagementSystem.Cloud.dll"]
