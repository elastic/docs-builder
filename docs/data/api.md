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
