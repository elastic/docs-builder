#!/usr/bin/env bash
set -euo pipefail

COMPOSE_FILE="build/dev/docker-compose.yml"
BAKE_FILE="build/dev/docker-bake.hcl"

_compose() { docker compose --project-directory . --file "$COMPOSE_FILE" "$@"; }
_bake()    { docker buildx bake --file "$BAKE_FILE" "$@"; }

# Build an image from the bake file unless it already exists: _ensure <tag> <bake target>.
_ensure() {
  docker image inspect "docs-builder:$1" >/dev/null 2>&1 \
    || _bake --load "$2"
}

# The CLI clones with git@github.com: origins and the image has no ssh client.
# Rewrite them to HTTPS, with a token when one is available (needed for private
# repos): GITHUB_TOKEN, else `gh auth token`.
_git_https() {
  local token="${GITHUB_TOKEN:-}"
  if [ -z "$token" ] && command -v gh >/dev/null 2>&1; then
    token="$(gh auth token 2>/dev/null || true)"
  fi
  export GIT_CONFIG_COUNT=1
  export GIT_CONFIG_KEY_0="url.https://${token:+oauth2:$token@}github.com/.insteadOf"
  export GIT_CONFIG_VALUE_0="git@github.com:"
}

# Assembler and codex run from source in the tooling image. Their services use
# the named .artifacts volumes. Clone and build publish no ports, so they never
# collide with a running serve.
_cli() {
  _git_https
  _ensure tooling tooling
  _compose run --rm "$@"
}

# Serve flows publish the fixed host port from the compose file (4000 for
# assembler, 4001 for codex), which maps to container port 4000. A forwarded
# --port would move the app off that mapping, so reject it.
# Usage: _cli_serve <url> <docker compose run args...>
_cli_serve() {
  local url="$1" arg
  shift
  for arg in "$@"; do
    case "$arg" in
      --port|--port=*|-p)
        echo "error: --port is fixed for dev.sh serve commands; the compose file publishes a fixed port." >&2
        exit 1
        ;;
    esac
  done
  echo "Serving at $url"
  _git_https
  _ensure tooling tooling
  _compose run --rm --service-ports "$@"
}

CODEX_CONFIG="${CODEX_CONFIG:-config/codex.example.yml}"

# Host ports, one per mode (see the table in docker-compose.yml).
ISOLATED_URL="http://localhost:3000"
ASSEMBLER_URL="http://localhost:4000"
CODEX_URL="http://localhost:4001"

case "${1:-help}" in
  help|--help|-h)
    echo "Usage: ./dev.sh <command>"
    echo ""
    echo "Each local mode has the same verbs: <mode>-serve, <mode>-build, <mode>-watch,"
    echo "plus <mode>-clone for the modes that clone repositories."
    echo ""
    echo "isolated: one docset (docs/), served at $ISOLATED_URL"
    echo "  isolated-serve           Serve with hot reload"
    echo "  isolated-watch           Same as isolated-serve"
    echo "  isolated-serve-detached  Serve in the background"
    echo "  isolated-stop            Stop the background server"
    echo "  isolated-build           Build the current documentation set"
    echo ""
    echo "assembler: every repository under one navigation, served at $ASSEMBLER_URL"
    echo "  assembler                Clone, build, and serve"
    echo "  assembler-clone          Clone the assembler repositories"
    echo "  assembler-build          Build the assembled site from the clones"
    echo "  assembler-serve          Serve the last build"
    echo "  assembler-watch          Rebuild and serve on every source change"
    echo ""
    echo "codex: the codex portal (config: \$CODEX_CONFIG), served at $CODEX_URL"
    echo "  codex                    Clone, build, and serve"
    echo "  codex-clone              Clone the codex repositories"
    echo "  codex-build              Build the codex from the clones"
    echo "  codex-serve              Serve the last build"
    echo "  codex-watch              Rebuild and serve on every source change"
    echo ""
    echo "Images and tests:"
    echo "  build                    Build the dev Docker images"
    echo "  build-api                Build only the docs API Docker image"
    echo "  build-mcp                Build only the MCP server Docker image"
    echo "  rebuild                  Rebuild the dev Docker images without cache"
    echo "  test                     Run the unit-test suite (delegates to ./build.sh unit-test)"
    echo ""
    echo "Other services:"
    echo "  serve-api                Run the docs API at http://localhost:8081"
    echo "  serve-api-detached       Run the docs API in the background"
    echo "  stop-api                 Stop the docs API"
    echo "  serve-mcp                Run the MCP server at http://localhost:8080 (public profile)"
    echo "  serve-mcp-internal       Run the MCP server at http://localhost:8080 (internal profile)"
    echo "  serve-mcp-detached       Run the MCP server in the background"
    echo "  stop-mcp                 Stop the MCP server"
    echo ""
    echo "  clean                    Stop containers and remove containers, networks, and volumes"
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
  test)
    _ensure tooling tooling
    _compose run --rm tests
    ;;

  isolated-serve|isolated-watch)
    _ensure tooling tooling
    echo "Serving at $ISOLATED_URL"
    _compose up --no-build serve
    ;;
  isolated-serve-detached)
    _ensure tooling tooling
    echo "Serving at $ISOLATED_URL"
    _compose up --no-build -d serve
    ;;
  isolated-stop)
    _compose stop serve
    ;;
  isolated-build)
    _ensure local runtime
    _compose run --rm docs-builder build
    ;;

  assembler)
    _cli_serve "$ASSEMBLER_URL" assembler assemble --serve "${@:2}"
    ;;
  assembler-clone)
    _cli assembler assembler clone "${@:2}"
    ;;
  assembler-build)
    _cli assembler assembler build "${@:2}"
    ;;
  assembler-serve)
    _cli_serve "$ASSEMBLER_URL" assembler assembler serve "${@:2}"
    ;;
  assembler-watch)
    _cli_serve "$ASSEMBLER_URL" --entrypoint sh assembler build/dev/watch-entrypoint.sh assembler "${@:2}"
    ;;

  codex)
    _cli_serve "$CODEX_URL" codex codex "$CODEX_CONFIG" --serve "${@:2}"
    ;;
  codex-clone)
    _cli codex codex clone "$CODEX_CONFIG" "${@:2}"
    ;;
  codex-build)
    _cli codex codex build "$CODEX_CONFIG" "${@:2}"
    ;;
  codex-serve)
    _cli_serve "$CODEX_URL" codex codex serve "${@:2}"
    ;;
  codex-watch)
    _cli_serve "$CODEX_URL" --entrypoint sh -e CODEX_CONFIG="$CODEX_CONFIG" codex build/dev/watch-entrypoint.sh codex "${@:2}"
    ;;

  serve-api)
    _ensure api api
    _compose up --no-build api
    ;;
  serve-api-detached)
    _ensure api api
    _compose up --no-build -d api
    ;;
  stop-api)
    _compose stop api
    ;;
  serve-mcp)
    _ensure mcp mcp
    _compose up --no-build mcp
    ;;
  serve-mcp-internal)
    _ensure mcp mcp
    MCP_SERVER_PROFILE=internal _compose up --no-build mcp
    ;;
  serve-mcp-detached)
    _ensure mcp mcp
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
