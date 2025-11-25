FROM public.ecr.aws/amazonlinux/amazonlinux:2023 AS base

ARG TARGETARCH
ARG TARGETOS

WORKDIR /app

# Set environment variables early (no layer cost)
ENV PATH="$PATH:/root/.dotnet" \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

# Install all system dependencies in a single layer
RUN rpm --import https://packages.microsoft.com/keys/microsoft.asc && \
    dnf update -y && \
    dnf install -y npm git clang tar findutils && \
    dnf clean all

# Install .NET SDK based on global.json version
COPY global.json .
RUN curl -L https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh && \
    chmod +x ./dotnet-install.sh && \
    ./dotnet-install.sh --jsonfile ./global.json && \
    rm dotnet-install.sh

# Copy solution/project files for dependency restoration
# .dockerignore already excludes bin/, obj/, node_modules/, etc.
# This includes all .csproj, .fsproj, .props, and source files
# dotnet restore only reads project files, so having source files here is fine
COPY . .

# Restore dependencies
# Note: We use cache mount for NuGet packages, but don't use --no-restore in publish
# because Native AOT compilation needs the full MSBuild evaluation
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/infra/docs-lambda-index-publisher -r linux-x64

# Build and publish (Native AOT requires full evaluation, so we don't use --no-restore)
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/infra/docs-lambda-index-publisher -r linux-x64 -c Release
