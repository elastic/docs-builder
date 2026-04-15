---
navigation_title: Elasticsearch API Reference
applies_to:
  stack: ga 8.0
---

# Elasticsearch API Reference

Welcome to the Elasticsearch API documentation. This guide covers all available REST endpoints for interacting with Elasticsearch clusters.

## Getting Started

Before using the Elasticsearch APIs, ensure you have:

1. A running Elasticsearch cluster
2. Proper authentication configured
3. Network access to the cluster

:::{tip}
All examples in this documentation assume you're using `curl` from a command line. You can also use any HTTP client that supports REST API calls.
:::

## Authentication

Most Elasticsearch APIs require authentication. The preferred method is API key authentication:

```bash
curl -X GET "localhost:9200/_cluster/health" \
     -H "Authorization: ApiKey <your-api-key>"
```

## API Categories

The Elasticsearch APIs are organized into the following categories:

### Search APIs
Use these APIs to search and analyze your data.

{{% api-operations-nav tag="search" %}}

### Index Management APIs
APIs for creating, configuring, and managing indices.

{{% api-operations-nav tag="indices" %}}

### Cluster Management APIs
APIs for monitoring and managing your Elasticsearch cluster.

{{% api-operations-nav tag="cluster" %}}

### Document APIs
APIs for indexing, updating, and deleting individual documents.

{{% api-operations-nav tag="document" %}}

## Response Format

All Elasticsearch APIs return responses in JSON format. A typical successful response includes:

```json
{
  "acknowledged": true,
  "shards_acknowledged": true,
  "index": "my-index"
}
```

## Error Handling

When an API call fails, Elasticsearch returns an error response with details:

```json
{
  "error": {
    "type": "index_not_found_exception",
    "reason": "no such index [missing-index]",
    "index": "missing-index"
  }
}
```

## Rate Limiting

Be aware of rate limiting when making API calls:

- Use bulk APIs for multiple operations
- Implement exponential backoff for retries
- Monitor cluster health before making requests

## Next Steps

- Browse the API categories above to find specific endpoints
- Check the {{stack}} compatibility matrix for version requirements
- Review the [security documentation](docs-content://security/index.md) for authentication details