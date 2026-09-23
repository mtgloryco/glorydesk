#!/usr/bin/env bash
# Builds the Browser (WASM) project and serves it as static files so it can be opened in a
# real browser. Do NOT use "dotnet run" here — that invokes WasmAppHost, a headless test
# runner, not a dev server, and fails with "no perHostConfigs found".
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
PROJECT_DIR="$REPO_ROOT/GloryDesk.Browser"
APP_BUNDLE="$PROJECT_DIR/bin/Debug/net10.0-browser/browser-wasm/AppBundle"
PORT="${1:-8080}"

echo "Building GloryDesk.Browser..."
dotnet build "$PROJECT_DIR/GloryDesk.Browser.csproj"

echo ""
echo "Serving $APP_BUNDLE on http://localhost:$PORT"
echo "Open that URL in a real browser (Chrome/Firefox), not headless. Ctrl+C to stop."
cd "$APP_BUNDLE"
python3 -m http.server "$PORT"
