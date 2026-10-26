FROM public.ecr.aws/amazonlinux/amazonlinux:2023 AS base

ARG TARGETARCH
ARG TARGETOS

WORKDIR /app

RUN rpm --import https://packages.microsoft.com/keys/microsoft.asc

RUN dnf update -y \
    && dnf install -y git clang libicu nodejs22 nodejs22-npm

# dotnet-install.sh requires tar and findutils, not present in the AL2023 base image
# see: https://github.com/amazonlinux/amazon-linux-2023/issues/1025
RUN dnf install -y tar findutils \
    && curl -L https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh
ENV PATH="$PATH:/root/.dotnet"

COPY . .

ENV DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

RUN arch=$TARGETARCH \
    && if [ "$arch" = "amd64" ]; then arch="x64"; fi \
    && RID="${TARGETOS}-${arch}" \
    && dotnet publish src/api/Elastic.Documentation.Api -r "${RID}" -c Release \
    && cp -r ".artifacts/publish/Elastic.Documentation.Api/release_${RID}/." /app/publish/

# Runtime stage
FROM public.ecr.aws/amazonlinux/amazonlinux:2023-minimal AS runtime
WORKDIR /app
COPY --from=base /app/publish/ /app/
RUN chmod +x /app/docs-builder-api
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENTRYPOINT ["/app/docs-builder-api"]
