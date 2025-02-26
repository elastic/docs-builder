FROM public.ecr.aws/amazonlinux/amazonlinux:2023 AS base

WORKDIR /app

RUN rpm --import https://packages.microsoft.com/keys/microsoft.asc

RUN curl -o /etc/yum.repos.d/microsoft-prod.repo https://packages.microsoft.com/config/fedora/39/prod.repo

RUN dnf update -y
RUN dnf install dotnet-sdk-9.0

COPY ./src/ ./src/
COPY ./*.sln ./

ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

RUN dotnet restore

COPY ./src/ ./src/
RUN dotnet publish ./src/Elastic.Documenation.Lambda.LinkIndexUploader -r linux-x64 -c Release -o ./out/upload-handler