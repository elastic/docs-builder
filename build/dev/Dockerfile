# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build

WORKDIR /src

ARG TARGETARCH

# The documentation site is built as part of the docs-builder project, and
# Native AOT compilation requires a C/C++ toolchain. .nvmrc is the single
# source of the Node version, so the official binary is installed from it.
COPY .nvmrc ./
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates clang curl git xz-utils \
    && rm -rf /var/lib/apt/lists/* \
    && NODE_ARCH=$([ "$TARGETARCH" = "arm64" ] && echo arm64 || echo x64) \
    && NODE_VERSION=$(tr -d 'v\n ' < .nvmrc) \
    && curl -fsSL "https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-${NODE_ARCH}.tar.xz" \
    | tar -xJ -C /usr/local --strip-components=1 \
    && node --version && npm --version

# global.json is the single source of the SDK version. The base image only
# supplies the OS and host; this installs the exact SDK the repo pins beside it.
COPY global.json ./
RUN curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh --jsonfile global.json --install-dir "${DOTNET_ROOT:-/usr/share/dotnet}" \
    && rm /tmp/dotnet-install.sh

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

FROM mcr.microsoft.com/dotnet/runtime-deps:11.0 AS runtime

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
