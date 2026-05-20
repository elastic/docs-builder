FROM public.ecr.aws/amazonlinux/amazonlinux:2023 AS base

ARG TARGETARCH
ARG TARGETOS

WORKDIR /app

RUN rpm --import https://packages.microsoft.com/keys/microsoft.asc

RUN dnf update -y
RUN dnf install -y npm
RUN dnf install -y git
RUN dnf install -y clang

## temporary see: https://github.com/amazonlinux/amazon-linux-2023/issues/1025
RUN dnf install -y tar
RUN dnf install -y findutils
RUN curl -L https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
RUN chmod +x ./dotnet-install.sh
RUN ./dotnet-install.sh
ENV PATH="$PATH:/root/.dotnet"
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
# /end temporary

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
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["/app/docs-builder-api"]
