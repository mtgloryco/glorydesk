# syntax=docker/dockerfile:1
# Builds and serves GloryDesk.Browser (the Avalonia WASM frontend) as a
# static site for Railway. Build context must stay the REPO ROOT (this project references
# ../GloryDesk.Shared as a sibling folder), even though this Dockerfile lives
# under GloryDesk.Browser/. In Railway, set:
#   Root Directory        -> / (repo root)
#   Custom Dockerfile Path -> GloryDesk.Browser/Dockerfile

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# python3 is required by Emscripten to compile the native WASM runtime bits.
RUN apt-get update && apt-get install -y --no-install-recommends python3 \
    && rm -rf /var/lib/apt/lists/*

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet workload install wasm-tools

COPY Directory.Build.props ./
COPY GloryDesk.Shared/GloryDesk.Shared.csproj GloryDesk.Shared/
COPY GloryDesk.Browser/GloryDesk.Browser.csproj GloryDesk.Browser/
RUN dotnet restore GloryDesk.Browser/GloryDesk.Browser.csproj

COPY GloryDesk.Shared/ GloryDesk.Shared/
COPY GloryDesk.Browser/ GloryDesk.Browser/
RUN dotnet publish GloryDesk.Browser/GloryDesk.Browser.csproj \
    -c Release --no-restore

FROM nginx:1.27-alpine AS runtime

# Only substitute $PORT — leaves nginx's own $uri/$host/etc. variables alone.
ENV NGINX_ENVSUBST_FILTER=PORT
ENV PORT=8080

COPY GloryDesk.Browser/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /src/GloryDesk.Browser/bin/Release/net10.0-browser/browser-wasm/AppBundle /usr/share/nginx/html

EXPOSE 8080
