FROM public.ecr.aws/amazonlinux/amazonlinux:2023 AS base

ARG TARGETARCH
ARG TARGETOS

WORKDIR /app

RUN rpm --import https://packages.microsoft.com/keys/microsoft.asc

#RUN curl -o /etc/yum.repos.d/microsoft-prod.repo https://packages.microsoft.com/config/fedora/41/prod.repo

RUN dnf update -y \
    && dnf install -y git clang libicu tar xz findutils

# .nvmrc is the single source of the Node version. AL2023 does not package Node 26.
COPY .nvmrc .
RUN NODE_ARCH=$([ "$TARGETARCH" = "arm64" ] && echo arm64 || echo x64) \
    && NODE_VERSION=$(tr -d 'v\n ' < .nvmrc) \
    && curl -fsSL "https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-${NODE_ARCH}.tar.xz" \
    | tar -xJ -C /usr/local --strip-components=1 \
    && node --version && npm --version

# dotnet-install.sh requires tar and findutils, not present in the AL2023 base image
# see: https://github.com/amazonlinux/amazon-linux-2023/issues/1025
# global.json is the single source of the SDK version, so it is copied before the install.
COPY global.json .
RUN curl -L https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh --jsonfile global.json
ENV PATH="$PATH:/root/.dotnet"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

COPY . .

ENV DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

RUN arch=$TARGETARCH \
    && if [ "$arch" = "amd64" ]; then arch="x64"; fi \
    && echo $TARGETOS-$arch > /tmp/rid

RUN dotnet publish src/infra/docs-lambda-index-publisher -r linux-x64 -c Release
