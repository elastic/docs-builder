---
navigation_title: REST API
---

# REST API

{{dbuild}} includes a REST API server (`Elastic.Documentation.Api`) that powers search, AI assistance, and content change feeds for the documentation site.

## Endpoints

| Endpoint | Description |
|----------|-------------|
| Search | Full-text and semantic search across all published documentation |
| Navigation search | Search scoped to navigation structure and page titles |
| Ask AI | Streaming endpoint for AI-powered question answering over documentation |
| Changes feed | Content change feed for tracking documentation updates |
| APIs and API operations | Search, read, and list the REST API operations that API Explorer publishes |

## APIs and API operations

Two resources describe the REST APIs that API Explorer publishes: `apis` and `api-operations`. They answer the same lookups as the `search_api_operations`, `get_api_operation`, and `list_api_operations` tools of the [MCP server](mcp/index.md), and serve HTTP clients such as the Elastic CLI's `elastic docs api` commands. All requests are `GET` under `/docs/_api/v1`, and responses are camelCase JSON.

An operation's id is `{api}:{operationId}`, such as `elasticsearch:cluster-health`. Every operation in a response carries it as `id`. API keys never contain `:`, so an operation id after the first `:` may.

`api` is an API key such as `elasticsearch` or `kibana`. `version` is `main` or a major such as `9`, and needs `api`. Without `version`, a request reads the API's current version.

### APIs

| Request | Query parameters | Returns |
|---------|------------------|---------|
| `/apis` | None | `apis`, with each API's key, title, landing page, and versions |
| `/apis/{api}` | None | That API's key, title, landing page, and versions |
| `/apis/{api}/groups` | `version` | The API's `groups`, each with its name and operation count |

### API operations

`/api-operations` is one collection. It takes at most one of `q`, `endpoint`, or `operation_id`. Without any of them, it lists one API's operations and needs `api`.

| Query parameter | Meaning |
|-----------------|---------|
| `q` | Words, a route such as `GET /_cluster/health`, or a field path. A trailing dot, as in `retriever.`, lists child fields. Results come in relevance order with a `score` and the `matchedFields`. |
| `endpoint` | A request such as `GET /my-index/_search`, with or without the method. Results are the operations that serve it, best match first. |
| `operation_id` | An operation id without its API, such as `cluster-health`. Results are that id in every API, or in `api` only. |
| `api`, `version` | Scope to one API and version |
| `group` | Lists one group of the API. It doesn't combine with `q`, `endpoint`, or `operation_id`. |
| `include_deprecated` | Include deprecated operations in a search or listing. The default is `false`. |
| `view` | `basic` (the default) or `full`. `full` adds each result's `versions` and `fields`, and needs `endpoint` or `operation_id`. |
| `include_body` | Adds each result's `body`. It needs `view=full`. |
| `page`, `size` | Page through a search or listing. `size` defaults to `10` and is at most `50`. |

The collection returns `totalHits` and `results`. Each result has its `operation`, plus the fields its filters add. A lookup that matches nothing returns an empty page, not `404`.

`/api-operations/{id}` reads one operation by its id. It takes `version` and `include_body` (default `false`). It returns the `operation`, its `versions`, every documented field in `fields`, `bodyLength`, `otherMatches`, and `body` only when `include_body=true`.

### Errors

A request these resources can't answer returns `{ "errorType": "...", "message": "..." }`. The status is `404` when `errorType` ends in `_not_found`: `operation_not_found`, `api_not_found`, or `group_not_found`. Invalid arguments return `400` with `invalid_arguments`.

## Ask AI provider

Each deployment of the API server picks its Ask AI provider with the `DOCUMENTATION_ASK_AI_PROVIDER` setting. A request can't change it.

| Value | Provider |
|-------|----------|
| Unset or empty | LLM Gateway (the default) |
| `LlmGateway` | LLM Gateway |
| `AgentBuilder` | Agent Builder |

The value is case-insensitive. Any other value stops the Ask AI service with a configuration error. LLM Gateway needs `LLM_GATEWAY_FUNCTION_URL` and `LLM_GATEWAY_SERVICE_ACCOUNT`. Agent Builder needs `DOCUMENTATION_KIBANA_URL` and `DOCUMENTATION_KIBANA_APIKEY`. A deployment that uses Agent Builder, such as Codex, must set `DOCUMENTATION_ASK_AI_PROVIDER=AgentBuilder`.

:::{note}
A formal OpenAPI specification should be generated from the API server as a follow-up. This page will be updated with full endpoint details, request/response schemas, and authentication requirements once the spec is available.
:::
