#!/usr/bin/env bash
set -euo pipefail

COMPOSE_FILE="build/dev/docker-compose.yml"
BAKE_FILE="build/dev/docker-bake.hcl"

_compose() { docker compose --project-directory . --file "$COMPOSE_FILE" "$@"; }
_bake()    { docker buildx bake --file "$BAKE_FILE" "$@"; }

_ensure_tooling() {
  docker image inspect docs-builder:tooling >/dev/null 2>&1 \
    || _bake --load tooling
}

_ensure_runtime() {
  docker image inspect docs-builder:local >/dev/null 2>&1 \
    || _bake --load runtime
}

_ensure_mcp() {
  docker image inspect docs-builder:mcp >/dev/null 2>&1 \
    || _bake --load mcp
}

case "${1:-help}" in
  help|--help|-h)
    echo "Usage: ./dev.sh <command>"
    echo ""
    echo "  build                Build the dev Docker images"
    echo "  build-mcp            Build only the MCP server Docker image"
    echo "  rebuild              Rebuild the dev Docker images without cache"
    echo "  serve                Serve docs at http://localhost:3000 with hot reload"
    echo "  serve-detached       Serve docs in the background"
    echo "  stop                 Stop the development server"
    echo "  docs                 Build the current documentation set"
    echo "  test                 Run the unit-test suite (delegates to ./build.sh unit-test)"
    echo "  serve-mcp            Run the MCP server at http://localhost:8080"
    echo "  serve-mcp-detached   Run the MCP server in the background"
    echo "  stop-mcp             Stop the MCP server"
    echo "  clean                Stop containers and remove containers, networks, and volumes"
    ;;
  build)
    _bake --load all
    ;;
  build-mcp)
    _bake --load mcp "${@:2}"
    ;;
  rebuild)
    _bake --no-cache --load all
    ;;
  serve)
    _ensure_tooling
    _compose up --no-build serve
    ;;
  serve-detached)
    _ensure_tooling
    _compose up --no-build -d serve
    ;;
  stop)
    _compose stop serve
    ;;
  docs)
    _ensure_runtime
    _compose run --rm docs-builder build
    ;;
  test)
    _ensure_tooling
    _compose run --rm tests
    ;;
  serve-mcp)
    _ensure_mcp
    _compose up --no-build mcp
    ;;
  serve-mcp-detached)
    _ensure_mcp
    _compose up --no-build -d mcp
    ;;
  stop-mcp)
    _compose stop mcp
    ;;
  clean)
    _compose down --remove-orphans --volumes
    ;;
  *)
    echo "error: unknown command '$1'" >&2
    echo "Run './dev.sh help' for usage." >&2
    exit 1
    ;;
esac
