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

# Assembler and codex run from source in the tooling image. Their services use
# the named .artifacts volumes; --service-ports publishes the serve port.
_cli() {
  _ensure_tooling
  _compose run --rm --service-ports "$@"
}

CODEX_CONFIG="${CODEX_CONFIG:-config/codex.example.yml}"

_ensure_runtime() {
  docker image inspect docs-builder:local >/dev/null 2>&1 \
    || _bake --load runtime
}

_ensure_mcp() {
  docker image inspect docs-builder:mcp >/dev/null 2>&1 \
    || _bake --load mcp
}

_ensure_api() {
  docker image inspect docs-builder:api >/dev/null 2>&1 \
    || _bake --load api
}

case "${1:-help}" in
  help|--help|-h)
    echo "Usage: ./dev.sh <command>"
    echo ""
    echo "  build                Build the dev Docker images"
    echo "  build-api            Build only the docs API Docker image"
    echo "  build-mcp            Build only the MCP server Docker image"
    echo "  rebuild              Rebuild the dev Docker images without cache"
    echo "  serve                Serve docs at http://localhost:3000 with hot reload"
    echo "  serve-detached       Serve docs in the background"
    echo "  stop                 Stop the development server"
    echo "  docs                 Build the current documentation set"
    echo "  test                 Run the unit-test suite (delegates to ./build.sh unit-test)"
    echo "  serve-api            Run the docs API at http://localhost:8081"
    echo "  serve-api-detached   Run the docs API in the background"
    echo "  assembler            Clone, build, and serve the assembled site at http://localhost:4000"
    echo "  assembler-clone      Clone the assembler repositories"
    echo "  assembler-build      Build the assembled site from the clones"
    echo "  assembler-serve      Serve the last assembler build at http://localhost:4000"
    echo "  codex                Clone, build, and serve the codex at http://localhost:4001"
    echo "  codex-clone          Clone the codex repositories (config: \$CODEX_CONFIG)"
    echo "  codex-build          Build the codex from the clones"
    echo "  codex-serve          Serve the last codex build at http://localhost:4001"
    echo "  stop-api             Stop the docs API"
    echo "  serve-mcp            Run the MCP server at http://localhost:8080 (public profile)"
    echo "  serve-mcp-internal   Run the MCP server at http://localhost:8080 (internal profile)"
    echo "  serve-mcp-detached   Run the MCP server in the background"
    echo "  stop-mcp             Stop the MCP server"
    echo "  clean                Stop containers and remove containers, networks, and volumes"
    ;;
  build)
    _bake --load all
    ;;
  build-api)
    _bake --load api "${@:2}"
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
  serve-api)
    _ensure_api
    _compose up --no-build api
    ;;
  serve-api-detached)
    _ensure_api
    _compose up --no-build -d api
    ;;
  stop-api)
    _compose stop api
    ;;
  serve-mcp)
    _ensure_mcp
    _compose up --no-build mcp
    ;;
  serve-mcp-internal)
    _ensure_mcp
    MCP_SERVER_PROFILE=internal _compose up --no-build mcp
    ;;
  serve-mcp-detached)
    _ensure_mcp
    _compose up --no-build -d mcp
    ;;
  stop-mcp)
    _compose stop mcp
    ;;
  assembler)
    _cli assembler assemble --serve "${@:2}"
    ;;
  assembler-clone)
    _cli assembler assembler clone "${@:2}"
    ;;
  assembler-build)
    _cli assembler assembler build "${@:2}"
    ;;
  assembler-serve)
    _cli assembler assembler serve "${@:2}"
    ;;
  codex)
    _cli codex codex "$CODEX_CONFIG" --serve "${@:2}"
    ;;
  codex-clone)
    _cli codex codex clone "$CODEX_CONFIG" "${@:2}"
    ;;
  codex-build)
    _cli codex codex build "$CODEX_CONFIG" "${@:2}"
    ;;
  codex-serve)
    _cli codex codex serve "${@:2}"
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
