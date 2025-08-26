# Contributing

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [Node.js 22.13.1 (LTS)](https://nodejs.org/en/blog/release/v22.13.1)
  - [Aspire 9.4.1](https://learn.microsoft.com/en-us/dotnet/aspire/)
	```bash
	dotnet workload install aspire
	```

## Validate the fully assembled documentation

```bash
dotnet run --project aspire
```

Will spin up all our services and clone and build all the documentation sets. 

```markdown
dotnet run --project aspire -- --assume-cloned --skip-private-repositories
```

`--asume-cloned` will assume a documentation set set is already cloned if available locally.

`--skip-private-repositories` will skip cloning private repositories. It will also inject our `docs-builder docs into the 
navigation. This allows us to validate new features' effect on the assembly process.

Our [Integration Tests](./tests-integration/Elastic.Assembler.IntegrationTests) use this exact command to validate the 
assembler builds.

## Continuously build all assets during development.

```shell
./build.sh watch
```

This will monitor code, cshtml template files & static files and reload the application
if any changes.

Web assets are reloaded through `parcel watch` and don't require a recompilation.

Markdown files are refreshed automatically through livereload

Code or layout changes will relaunch the server automatically

# Release Process

This section outlines the process for releasing a new version of this project.

## Versioning

This project uses [Semantic Versioning](https://semver.org/) and its version is
automatically determined by [release-drafter](https://github.com/release-drafter/release-drafter)
based on the labels of the pull requests merged into the `main` branch.

See the [release-drafter configuration](./.github/release-drafter.yml) for more details.

## Creating a New Release

To create a new release trigger the [release](https://github.com/elastic/docs-builder/actions/workflows/release.yml) workflow on the `main` branch.

Every time a pull request is merged into the `main` branch, release-drafter will
create a draft release or update the existing draft release in the [Releases](https://github.com/elastic/docs-builder/releases) page.

To create a new release you need to publish the existing draft release created by release-drafter.

> [!IMPORTANT]
> Make sure the [release-drafter workflow](https://github.com/elastic/docs-builder/actions/workflows/release-drafter.yml) is finished before publishing the release.

> [!NOTE]
> When a release is published, the [create-major-tag workflow](./.github/workflows/create-major-tag.yml)
> will force push a new major tag in the format `vX` where `X` is the major version of the release.
> For example, if the release is `1.2.3` was published, the workflow will force push a new tag `v1` on the same commit.
