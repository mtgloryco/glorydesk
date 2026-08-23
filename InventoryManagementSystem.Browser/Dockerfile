# syntax=docker/dockerfile:1
# Builds and serves InventoryManagementSystem.Browser (the Avalonia WASM frontend) as a
# static site for Railway. Build context must stay the REPO ROOT (this project references
# ../InventoryManagementSystem.Shared as a sibling folder), even though this Dockerfile lives
# under InventoryManagementSystem.Browser/. In Railway, set:
#   Root Directory        -> / (repo root)
#   Custom Dockerfile Path -> InventoryManagementSystem.Browser/Dockerfile

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# python3 is required by Emscripten to compile the native WASM runtime bits.
RUN apt-get update && apt-get install -y --no-install-recommends python3 \
    && rm -rf /var/lib/apt/lists/*

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet workload install wasm-tools

COPY Directory.Build.props ./
COPY InventoryManagementSystem.Shared/InventoryManagementSystem.Shared.csproj InventoryManagementSystem.Shared/
COPY InventoryManagementSystem.Browser/InventoryManagementSystem.Browser.csproj InventoryManagementSystem.Browser/
RUN dotnet restore InventoryManagementSystem.Browser/InventoryManagementSystem.Browser.csproj

COPY InventoryManagementSystem.Shared/ InventoryManagementSystem.Shared/
COPY InventoryManagementSystem.Browser/ InventoryManagementSystem.Browser/
RUN dotnet publish InventoryManagementSystem.Browser/InventoryManagementSystem.Browser.csproj \
    -c Release --no-restore

FROM nginx:1.27-alpine AS runtime

# Only substitute $PORT — leaves nginx's own $uri/$host/etc. variables alone.
ENV NGINX_ENVSUBST_FILTER=PORT
ENV PORT=8080

COPY InventoryManagementSystem.Browser/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /src/InventoryManagementSystem.Browser/bin/Release/net10.0-browser/browser-wasm/AppBundle /usr/share/nginx/html

EXPOSE 8080
