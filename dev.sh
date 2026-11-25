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

ES_URL="http://localhost:9200"

# Start the local cluster and block until it answers. Local indexing passes
# --no-semantic, so nothing waits for an inference model.
_es_up() {
  _compose up -d elasticsearch
  local deadline=$((SECONDS + ${ES_WAIT_SECONDS:-120}))
  echo "Waiting for Elasticsearch at $ES_URL"
  until curl -sf --max-time 10 "$ES_URL/_cluster/health?wait_for_status=yellow&timeout=5s" >/dev/null; do
    [ "$SECONDS" -lt "$deadline" ] || { echo "error: Elasticsearch did not start" >&2; exit 1; }
    sleep 2
  done
  _es_licence_check "$deadline"
  echo "Elasticsearch ready at $ES_URL. Index the assembled docs with ./dev.sh es-index"
}

# Prints one string field of a JSON object, whatever the whitespace around the colon.
_json_field() {
  printf '%s' "$1" | tr -d '\n' | sed -n "s/.*\"$2\"[[:space:]]*:[[:space:]]*\"\([^\"]*\)\".*/\1/p"
}

# Search wraps every query in a query rule, which needs a trial (or higher) licence.
# The trial ends 30 days after the volume is created; a reset starts a new one.
_es_licence_check() {
  local deadline="$1" licence="" status="" type=""
  # A fresh cluster can answer 404 here while it installs its licence, so retry within the start-up wait.
  until [ -n "$status" ] && [ -n "$type" ]; do
    [ "$SECONDS" -lt "$deadline" ] || { echo "error: could not read the licence from $ES_URL/_license" >&2; exit 1; }
    licence="$(curl -sf --max-time 10 "$ES_URL/_license" || true)"
    status="$(_json_field "$licence" status)"
    type="$(_json_field "$licence" type)"
    [ -n "$status" ] && [ -n "$type" ] || sleep 2
  done
  if [ "$status" != "active" ] || [ "$type" = "basic" ]; then
    echo "error: the local Elasticsearch licence is $type ($status). Query rules, and with them search, need a trial." >&2
    echo "Run ./dev.sh es-reset (mise run es:reset) to start a fresh cluster with a new 30-day trial, then index again." >&2
    exit 1
  fi
  echo "Licence: $type, active until $(_json_field "$licence" expiry_date)"
}

# Delete the cluster and its data only, for example when the 30-day trial licence
# expires. `clean` would also drop the clones, build output and package caches.
_es_reset() {
  local project
  project="$(_compose config --format json | sed -n 's/^  "name": "\(.*\)",$/\1/p' | head -1)"
  _compose rm --stop --force elasticsearch
  docker volume rm --force "${project}_es-data" >/dev/null
  echo "Removed the local Elasticsearch and its data. Run ./dev.sh es-up to start a fresh one."
}

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
    echo "  es-up                    Start Elasticsearch at $ES_URL and wait until it answers"
    echo "  es-index                 Index the assembled docs, API pages included (run es-up and assembler-clone first)"
    echo "  es-stop                  Stop Elasticsearch"
    echo "  es-reset                 Delete Elasticsearch and its data, for example after the trial licence expires"
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
  es-up)
    _es_up
    ;;
  es-index)
    # Only assembler builds export API pages. --no-semantic writes the semantic index
    # without inference fields, so no model runs on the local cluster.
    _cli assembler assembler index --no-semantic --no-ai-enrichment "${@:2}"
    ;;
  es-stop)
    _compose stop elasticsearch
    ;;
  es-reset)
    _es_reset
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
