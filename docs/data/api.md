---
navigation_title: REST API
---

# REST API

{{dbuild}} includes a REST API server (`Elastic.Documentation.Api`) that powers search, AI assistance, and content change feeds for the documentation site.

The server describes itself in an OpenAPI document at `/docs/_api/v1/openapi.json`, generated from its endpoints. It lists every endpoint with its parameters, request bodies, responses, and error codes. Requests through `www.elastic.co` need the cookie `feature_search_or_askai_enabled=true`; without it, the CDN answers `403`.

## Endpoints

| Endpoint | Description |
|----------|-------------|
| Search | Full-text and semantic search across all published documentation |
| Navigation search | Search scoped to navigation structure and page titles |
| Ask AI | Streaming endpoint for AI-powered question answering over documentation |
| Changes feed | Content change feed for tracking documentation updates |
| APIs and API operations | Search, read, and list the REST API operations that API Explorer publishes |

## APIs and API operations

The `apis` resource describes the REST APIs that API Explorer publishes and, under each API, its operations. It answers the same lookups as the `search_api_operations`, `get_api_operation`, and `list_api_operations` tools of the [MCP server](mcp/index.md), and serves HTTP clients such as the Elastic CLI's `elastic docs api` commands. All requests are `GET` under `/docs/_api/v1`, and responses are camelCase JSON.

Every operation request names its API, such as `elasticsearch` or `kibana`. Many operation ids and routes exist in several APIs: `elasticsearch` and `elasticsearch-serverless` share most of theirs. A lookup across APIs would have to pick one, so there is none. The MCP tools still search across APIs when no API is given.

`version` is `main` or a major such as `9`. Without it, a request reads the API's current version.

### APIs

| Request | Query parameters | Returns |
|---------|------------------|---------|
| `/apis` | None | `apis`, with each API's key, title, landing page, and versions |
| `/apis/{api}` | None | That API's key, title, landing page, and versions |
| `/apis/{api}/groups` | `version`<br>`include_deprecated` (default `false`) | The API's `groups`, each with its name and operation count. Counts follow the same deprecated filter as a group listing. |

### Operations of an API

`/apis/{api}/operations` takes at most one of `q` or `endpoint`. Without either, it lists the API's operations by path.

| Query parameter | Meaning |
|-----------------|---------|
| `q` | Words, a route such as `GET /_cluster/health`, or a field path. A trailing dot, as in `retriever.`, lists child fields. Results come in relevance order with a `score` and the `matchedFields`. |
| `endpoint` | A request such as `GET /my-index/_search`, with or without the method. Results are the operations that serve it, best route first. A route made only of parameters, such as `/{index}`, can serve several. |
| `version` | The API version |
| `group` | Lists one group. It doesn't combine with `q` or `endpoint`. |
| `include_deprecated` | Include deprecated operations in a search or listing. The default is `false`. |
| `view` | `basic` (the default) or `full`. `full` adds each result's `versions` and `fields`, and needs `endpoint`. |
| `include_body` | Adds each result's `body`. It needs `view=full`. |
| `page`, `size` | Page through a search or listing. `size` defaults to `10` and is at most `50`. |

The response has `totalHits` and `results`. Each result has its `operation`, plus the fields its filters add. A lookup that matches nothing returns an empty page, not `404`.

`/apis/{api}/operations/{operationId}` reads one operation, such as `/apis/elasticsearch/operations/cluster-health`. It takes `version` and `include_body` (default `false`). It returns the `operation`, its `versions`, every documented field in `fields`, `bodyLength`, and `body` only when `include_body=true`.

### Errors

The API operations resources report errors like every other endpoint; see [Errors](#errors).

## Ask AI provider

Each deployment of the API server picks its Ask AI provider with the `DOCUMENTATION_ASK_AI_PROVIDER` setting. A request can't change it.

| Value | Provider |
|-------|----------|
| Unset or empty | LLM Gateway (the default) |
| `LlmGateway` | LLM Gateway |
| `AgentBuilder` | Agent Builder |

The value is case-insensitive. Any other value stops the Ask AI service with a configuration error. LLM Gateway needs `LLM_GATEWAY_FUNCTION_URL` and `LLM_GATEWAY_SERVICE_ACCOUNT`. Agent Builder needs `DOCUMENTATION_KIBANA_URL` and `DOCUMENTATION_KIBANA_APIKEY`. A deployment that uses Agent Builder, such as Codex, must set `DOCUMENTATION_ASK_AI_PROVIDER=AgentBuilder`.

## Errors

Every error is an [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) problem, served as `application/problem+json`:

```json
{
  "type": "https://docs-v3-preview.elastic.dev/elastic/docs-builder/tree/main/data/api#invalid-arguments",
  "title": "Invalid arguments",
  "status": 400,
  "detail": "Pass q or endpoint, not both.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

- `type` identifies the problem and links to its section below. Match on it, not on `title` or `detail`.
- `title` is the same for every occurrence of a type.
- `detail` explains this occurrence, for people.
- `traceId` names the request's trace. Quote it when you report a problem.

Errors from the CDN, such as the `403` for a missing cookie, aren't problems and aren't JSON.

### Invalid arguments [invalid-arguments]

`400`. A parameter is missing, doesn't parse, or contradicts another one. `detail` says which.

### Not found [not-found]

`404`. No endpoint matches the path.

### API operation not found [operation-not-found]

`404`. No API operation has that id, or serves that endpoint, in the requested API and version.

### API not found [api-not-found]

`404`. No API has that key, or it has no operations in the requested version.

### API group not found [group-not-found]

`404`. The API has no group with that name.

### Internal error [internal-error]

`500`. An unexpected failure. `detail` doesn't include its cause; use `traceId` to report it.

### Search unavailable [search-unavailable]

`503`. Elasticsearch timed out or is overloaded. Retry after the seconds in the `Retry-After` header.

### Feedback unavailable [feedback-unavailable]

`503`. Feedback could not be stored. Retry later.
