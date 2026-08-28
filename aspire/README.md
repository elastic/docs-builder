# Aspire for Elastic Documentation

We use [Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) for local development purposes to spin up all services in a controlled fashion.

> Aspire provides tools, templates, and packages for building observable, production-ready distributed apps. At the center is the app model—a code-first, single source of truth that defines your app's services, resources, and connections.  
>Aspire gives you a unified toolchain: launch and debug your entire app locally with one command, then deploy anywhere—Kubernetes, the cloud, or your own servers—using the same composition.

We do not use Aspire to generate production deployment scripts since [this is not fully baked for AWS and terraform yet](https://github.com/dotnet/aspire/issues/6559)

![service-graph.png](service-graph.png)

## Run all services locally

The Aspire toolchain ships as a local dotnet tool — no workload install needed. Restore once per clone:

```bash
dotnet tool restore
```

Then start all services:

```bash
dotnet aspire run
```

Or equivalently via `dotnet run`:

```bash
dotnet run --project aspire
```

This will automatically:

* reuse already-cloned repositories (default) or clone them if absent — via `docs-builder assembler clone --assume-cloned`
* build the unified site, skipping if the code/config/content stamp is unchanged — via `docs-builder assembler build`
* serve the fully assembled documentation via `docs-builder assembler serve`

This should start a management UI over at: https://localhost:17166. This UI exposes all logs, traces, and metrics for each service

![management-ui.png](management-ui.png)

### Default behaviour

Private repositories are **skipped by default** — `docs-builder`'s own docs are injected into `navigation.yml` in their place. This lets you validate the assembler without production credentials. Existing checkouts are **reused by default** (no fresh clone on every run). Build output is **skipped when unchanged** (MVID stamp matches code/config/content).

To change these defaults:

```bash
dotnet aspire run -- --no-skip-private-repositories  # include private repos (requires auth tokens)
dotnet aspire run -- --no-assume-cloned              # force a fresh clone
dotnet aspire run -- --no-assume-build               # force a full rebuild even if stamp matches
```

Our integration tests use these defaults to run tokenless on CI.

## Elasticsearch

All Elasticsearch connectivity targets **Elastic Cloud with EIS** (Elastic Inference Service). There is no local Elasticsearch container option — the index layout, inference endpoints, and semantic search configuration all require a Cloud deployment and cannot be replicated locally.

Configure your Cloud endpoint via user secrets (see [User secrets](#user-secrets) below).

## User secrets

We use the [Aspire CLI](https://aspire.dev/reference/cli/overview/) to manage secrets for the AppHost. Secrets are stored in the `docs-builder` dotnet user-secrets store.

```bash
dotnet aspire secret list
```

Should show:

> LlmGatewayUrl = https://****
> LlmGatewayServiceAccountPath = <PATH_TO_GCP_SERVICE_CREDENTIALS_FILE>
> ElasticsearchUrl = https://*.elastic.cloud:443
> ElasticsearchApiKey = ****

To set them:

```bash
dotnet aspire secret set ElasticsearchApiKey <VALUE>
dotnet aspire secret set ElasticsearchUrl <VALUE>
dotnet aspire secret set LlmGatewayUrl <VALUE>
dotnet aspire secret set LlmGatewayServiceAccountPath <VALUE>
```

Do note these secrets are only used on local development machines. CI fetches credentials from AWS SSM.

The store id is `docs-builder`. If you set up secrets before the rename from the old GUID id,
migrate your existing store:

```bash
mv ~/.microsoft/usersecrets/72f50f33-6fb9-4d08-bff3-39568fe370b3 ~/.microsoft/usersecrets/docs-builder
```

(On Windows: `%APPDATA%\Microsoft\UserSecrets\`.)

## Integration Tests

The `Elastic.Documentation.IntegrationTests` project includes integration tests that boot the full Aspire stack (clone → build → serve → api → mcp) and run liveness and smoke assertions against it.

### Running

```bash
dotnet test tests-integration/Elastic.Documentation.IntegrationTests
```

The tests use the default flags — `--skip-private-repositories` and `--assume-cloned` locally,
plus the MVID-based `--assume-build` on local (disabled automatically on CI). They require the
`ElasticsearchUrl` and `ElasticsearchApiKey` user secrets to be set for the API and MCP smoke assertions.

### Detached mode

Use `dotnet aspire start` to launch the stack in the background:

```bash
dotnet aspire start    # uses all defaults (skip private, reuse clones, stamp-based build skip)
dotnet aspire ps       # list running stacks
dotnet aspire stop     # shut down
```
