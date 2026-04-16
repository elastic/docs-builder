---
navigation_title: Kibana APIs
applies_to:
  stack: ga
  serverless: ga
---

# {{api.kibana.title}}

Welcome to the {{api.kibana.title}} documentation (version {{api.kibana.version}}). This page provides an overview of the available Kibana APIs.

:::{api-summary}
:product: kibana
:type: description
:::

## Kibana spaces

Spaces enable you to organize your dashboards and other saved objects into meaningful categories.
You can use the default space or create your own spaces.

To run APIs in non-default spaces, you must add `s/{space_id}/` to the path.
For example:

```bash
curl -X GET "http://${KIBANA_URL}/s/marketing/api/data_views" \
-H "Authorization: ApiKey ${API_KEY}"
```

If you use the Kibana console to send API requests, it automatically adds the appropriate space identifier.

To learn more, check out [Spaces](https://www.elastic.co/docs/deploy-manage/manage-spaces).

## Available operations

:::{api-summary}
:product: kibana
:type: operations
:::