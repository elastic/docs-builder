# syntax=docker/dockerfile:1

FROM node:22-bookworm-slim AS node

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# The documentation site is built as part of the docs-builder project, and
# Native AOT compilation requires a C/C++ toolchain.
COPY --from=node /usr/local/bin/node /usr/local/bin/node
COPY --from=node /usr/local/lib/node_modules /usr/local/lib/node_modules

RUN apt-get update \
    && apt-get install -y --no-install-recommends clang git \
    && rm -rf /var/lib/apt/lists/* \
    && ln -s /usr/local/lib/node_modules/npm/bin/npm-cli.js /usr/local/bin/npm \
    && ln -s /usr/local/lib/node_modules/npm/bin/npx-cli.js /usr/local/bin/npx

COPY . .

FROM build AS tooling

ARG TARGETARCH

# Keep the SDK dependency graph in the tooling image so dotnet watch does not
# need to restore packages every time the development container starts.
RUN --mount=type=cache,id=docs-builder-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore src/tooling/docs-builder/docs-builder.csproj

FROM tooling AS publish

RUN case "${TARGETARCH}" in \
      amd64) rid="linux-x64" ;; \
      arm64) rid="linux-arm64" ;; \
      *) echo "Unsupported Docker architecture: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && dotnet publish src/tooling/docs-builder/docs-builder.csproj \
    --configuration Release \
    --runtime "${rid}" \
    --self-contained true \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS runtime

WORKDIR /workspace

# Git is needed by commands such as `assemble`; ca-certificates is needed for
# fetching remote documentation and link indexes.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates git \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish/ /app/

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=true \
    DOTNET_NOLOGO=true

EXPOSE 3000

ENTRYPOINT ["/app/docs-builder"]
CMD ["--help"]
