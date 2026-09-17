FROM node:22-bookworm-slim AS node

FROM public.ecr.aws/amazonlinux/amazonlinux:2023 AS base

ARG TARGETARCH
ARG TARGETOS
# Override to pin a specific RID (e.g. linux-x64 in CI). When empty, the
# native Docker architecture is used (linux-arm64 on Apple Silicon, etc.).
ARG PUBLISH_RID=linux-x64

WORKDIR /app

RUN rpm --import https://packages.microsoft.com/keys/microsoft.asc

RUN dnf update -y
RUN dnf install -y git
RUN dnf install -y clang
RUN dnf install -y libicu

COPY --from=node /usr/local/bin/node /usr/local/bin/node
COPY --from=node /usr/local/lib/node_modules /usr/local/lib/node_modules
RUN ln -s /usr/local/lib/node_modules/npm/bin/npm-cli.js /usr/local/bin/npm \
    && ln -s /usr/local/lib/node_modules/npm/bin/npx-cli.js /usr/local/bin/npx

## temporary see: https://github.com/amazonlinux/amazon-linux-2023/issues/1025
RUN dnf install -y tar
RUN dnf install -y findutils
RUN curl -L https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
RUN chmod +x ./dotnet-install.sh
RUN ./dotnet-install.sh
ENV PATH="$PATH:/root/.dotnet"
# /end temporary

COPY . .

ENV DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

RUN if [ -n "$PUBLISH_RID" ]; then rid="$PUBLISH_RID"; \
    elif [ "$TARGETARCH" = "arm64" ]; then rid="linux-arm64"; \
    elif [ "$TARGETARCH" = "amd64" ]; then rid="linux-x64"; \
    else echo "Unknown TARGETARCH: $TARGETARCH" >&2; exit 1; fi \
    && dotnet publish src/api/Elastic.Documentation.Mcp.Remote \
       --configuration Release \
       --runtime "$rid" \
       --output /app/mcp-publish

# Runtime stage for ECS Fargate deployment
FROM public.ecr.aws/amazonlinux/amazonlinux:2023-minimal AS runtime
WORKDIR /app
COPY --from=base /app/mcp-publish/bootstrap /app/bootstrap
RUN chmod +x /app/bootstrap
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENTRYPOINT ["/app/bootstrap"]
