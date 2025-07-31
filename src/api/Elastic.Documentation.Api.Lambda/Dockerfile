FROM public.ecr.aws/amazonlinux/amazonlinux:2023 AS base

ARG TARGETARCH
ARG TARGETOS

WORKDIR /app

RUN rpm --import https://packages.microsoft.com/keys/microsoft.asc

RUN curl -o /etc/yum.repos.d/microsoft-prod.repo https://packages.microsoft.com/config/fedora/39/prod.repo

RUN dnf update -y
RUN dnf install -y dotnet-sdk-9.0
RUN dnf install -y npm
RUN dnf install -y git
RUN dnf install -y clang

COPY . .

ENV DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

RUN arch=$TARGETARCH \
    && if [ "$arch" = "amd64" ]; then arch="x64"; fi \
    && echo $TARGETOS-$arch > /tmp/rid

RUN dotnet publish src/api/AwsLambda -r linux-x64 -c Release
