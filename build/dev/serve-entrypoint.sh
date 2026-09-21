#!/bin/sh
set -e

# Keep node_modules in sync with package-lock.json. Keying on the lockfile
# hash catches branch switches and cross-OS leftovers without running npm ci
# on every start. The npm-cache volume (/root/.npm) retains downloaded
# packages so reinstalls after a lockfile change are fast.
LOCKFILE="src/Elastic.Documentation.Site/package-lock.json"
STAMP="src/Elastic.Documentation.Site/node_modules/.package-lock-hash"
CURRENT_HASH=$(sha256sum "$LOCKFILE" | cut -d' ' -f1)

if [ ! -f "$STAMP" ] || [ "$(cat "$STAMP")" != "$CURRENT_HASH" ]; then
    npm ci --prefix src/Elastic.Documentation.Site
    echo "$CURRENT_HASH" > "$STAMP"
fi

exec dotnet watch \
    --project src/tooling/docs-builder \
    --configuration debug \
    -- serve --watch --no-hud --port 3000
