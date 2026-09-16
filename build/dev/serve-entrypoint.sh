#!/bin/sh
set -e

# Install frontend dependencies on the first start. The npm-cache volume
# (/root/.npm) persists downloaded packages so subsequent starts are fast.
if [ ! -d src/Elastic.Documentation.Site/node_modules ]; then
    npm ci --prefix src/Elastic.Documentation.Site
fi

exec ./build.sh watch-docker
