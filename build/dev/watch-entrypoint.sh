#!/bin/sh
# Rebuild and serve the assembler or codex site whenever a source file changes.
#
# Usage: watch-entrypoint.sh assembler|codex [build args...]
#
# Each pass builds, then serves with --watch. The build replaces the whole
# output tree, so the old site is gone while it runs. The serve process is
# stopped for the build and restarted after it; open pages reconnect to the
# new process and reload.
set -u

site="${1:?usage: watch-entrypoint.sh assembler|codex [build args...]}"
shift

# The serve step always reads the default output directory, so a custom
# --output would build to one place and serve another.
for arg in "$@"; do
  case "$arg" in
    --output|--output=*|-o)
      echo "error: --output is not supported in watch mode; the server always serves the default output directory." >&2
      exit 1
      ;;
  esac
done

cli="dotnet run --project src/tooling/docs-builder --configuration debug"
marker=/tmp/watch-last-build
serve_pid=""

stop_serve() {
  if [ -n "$serve_pid" ]; then
    kill "$serve_pid" 2>/dev/null
    wait "$serve_pid" 2>/dev/null
    serve_pid=""
  fi
}

trap 'stop_serve; exit 0' INT TERM

build() {
  case "$site" in
    assembler) $cli -- assembler build "$@" ;;
    codex) $cli -- codex build "${CODEX_CONFIG:?CODEX_CONFIG is not set}" "$@" ;;
    *) echo "unknown site '$site'" >&2; exit 1 ;;
  esac
}

serve() {
  $cli --no-build -- "$site" serve --watch --port 4000 &
  serve_pid=$!
}

# Source and config the build reads. Build output folders are pruned so a
# build never triggers itself.
changed() {
  [ -n "$(find src config docs \
    \( -name node_modules -o -name bin -o -name obj -o -name _static -o -name .parcel-cache \) -prune -o \
    -type f -newer "$marker" \
    \( -name '*.cs' -o -name '*.cshtml' -o -name '*.ts' -o -name '*.tsx' -o -name '*.css' \
       -o -name '*.yml' -o -name '*.md' -o -name '*.csproj' -o -name '*.props' \) \
    -print -quit 2>/dev/null)" ]
}

while true; do
  # Stamp before building so an edit made mid-build triggers another pass.
  touch "$marker"
  if build "$@"; then
    serve
  else
    echo "Build failed. Fix the error and save a file to retry." >&2
  fi

  while ! changed; do
    sleep 2
  done

  echo "Change detected. Rebuilding..."
  stop_serve
done
