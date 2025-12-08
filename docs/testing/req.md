---
applies_to:
  stack: ga
  serverless:
    elasticsearch: ga
mapped_pages:
  - https://www.elastic.co/guide/en/elasticsearch/reference/current/es-connectors-usage.html
---
# Requirements

```{applies_to}
stack: preview =9.0, ga 9.1
```

1. Select **Create** to create a new policy, or select **Edit** {icon}`pencil` to open an existing policy.
1. Select **Create** to create a new policy, or select **Edit** {icon}`logo_vulnerability_management` to open an existing policy.


{applies_to}`stack: preview 9.0` This tutorial is based on Elasticsearch 9.0.
This tutorial is based on Elasticsearch 9.0. This tutorial is based on Elasticsearch 9.0.
This tutorial is based on Elasticsearch 9.0.

To follow this tutorial you will need to install the following components:


- An installation of Elasticsearch, based on our hosted [Elastic Cloud](https://www.elastic.co/cloud) service (which includes a free trial period), or a self-hosted service that you run on your own computer. See the Install Elasticsearch section above for installation instructions.
- A [Python](https://python.org) interpreter. Make sure it is a recent version, such as Python 3.8 or newer.

The tutorial assumes that you have no previous knowledge of Elasticsearch or general search topics, but it expects you to have a basic familiarity with the following technologies, at least at a beginner level:

- Python development
- The [Flask](https://flask.palletsprojects.com/) web framework for Python.
- The command prompt or terminal application in your operating system.


{applies_to}`ece: removed`

## Applies To Badge Scenarios

Below is a table of `applies_to` badge scenarios. 

### No version specified (serverless)

| Badge | Raw Markdown |
|-------|--------------|
| {applies_to}`serverless: ga` | ````markdown<br/>{applies_to}`serverless: ga`<br/>```` |
| {applies_to}`serverless: preview` | ````markdown<br/>{applies_to}`serverless: preview`<br/>```` |
| {applies_to}`serverless: beta` | ````markdown<br/>{applies_to}`serverless: beta`<br/>```` |
| {applies_to}`serverless: deprecated` | ````markdown<br/>{applies_to}`serverless: deprecated`<br/>```` |
| {applies_to}`serverless: removed` | ````markdown<br/>{applies_to}`serverless: removed`<br/>```` |

### No version specified (stack)

| Badge | Raw Markdown |
|-------|--------------|
| {applies_to}`stack: ga` | ````markdown<br/>{applies_to}`stack: ga`<br/>```` |
| {applies_to}`stack: preview` | ````markdown<br/>{applies_to}`stack: preview`<br/>```` |
| {applies_to}`stack: beta` | ````markdown<br/>{applies_to}`stack: beta`<br/>```` |
| {applies_to}`stack: deprecated` | ````markdown<br/>{applies_to}`stack: deprecated`<br/>```` |
| {applies_to}`stack: removed` | ````markdown<br/>{applies_to}`stack: removed`<br/>```` |

### Greater than or equal to (x.x+ / x.x)

| Badge | Raw Markdown |
|-------|--------------|
| {applies_to}`stack: ga 9.1` | ````markdown<br/>{applies_to}`stack: ga 9.1`<br/>```` |
| {applies_to}`stack: ga 9.1+` | ````markdown<br/>{applies_to}`stack: ga 9.1+`<br/>```` |
| {applies_to}`stack: preview 9.0+` | ````markdown<br/>{applies_to}`stack: preview 9.0+`<br/>```` |
| {applies_to}`stack: beta 9.1+` | ````markdown<br/>{applies_to}`stack: beta 9.1+`<br/>```` |
| {applies_to}`stack: deprecated 9.0+` | ````markdown<br/>{applies_to}`stack: deprecated 9.0+`<br/>```` |
| {applies_to}`stack: removed 9.0` | ````markdown<br/>{applies_to}`stack: removed 9.0`<br/>```` |

### Range (x.x-y.y)

| Badge | Raw Markdown |
|-------|--------------|
| {applies_to}`stack: ga 9.0-9.2` | ````markdown<br/>{applies_to}`stack: ga 9.0-9.2`<br/>```` |
| {applies_to}`stack: preview 9.0-9.2` | ````markdown<br/>{applies_to}`stack: preview 9.0-9.2`<br/>```` |
| {applies_to}`stack: beta 9.0-9.1` | ````markdown<br/>{applies_to}`stack: beta 9.0-9.1`<br/>```` |
| {applies_to}`stack: deprecated 9.0-9.2` | ````markdown<br/>{applies_to}`stack: deprecated 9.0-9.2`<br/>```` |

### Exact version (=x.x)

| Badge | Raw Markdown |
|-------|--------------|
| {applies_to}`stack: ga =9.1` | ````markdown<br/>{applies_to}`stack: ga =9.1`<br/>```` |
| {applies_to}`stack: preview =9.0` | ````markdown<br/>{applies_to}`stack: preview =9.0`<br/>```` |
| {applies_to}`stack: beta =9.1` | ````markdown<br/>{applies_to}`stack: beta =9.1`<br/>```` |
| {applies_to}`stack: deprecated =9.0` | ````markdown<br/>{applies_to}`stack: deprecated =9.0`<br/>```` |
| {applies_to}`stack: removed =9.0` | ````markdown<br/>{applies_to}`stack: removed =9.0`<br/>```` |

### Multiple lifecycles

| Badge | Raw Markdown |
|-------|--------------|
| {applies_to}`stack: ga 9.2+, beta 9.0-9.1` | ````markdown<br/>{applies_to}`stack: ga 9.2+, beta 9.0-9.1`<br/>```` |
| {applies_to}`stack: ga 9.2+, preview 9.0-9.1` | ````markdown<br/>{applies_to}`stack: ga 9.2+, preview 9.0-9.1`<br/>```` |

### Deployment types

| Badge | Raw Markdown |
|-------|--------------|
| {applies_to}`ece: ga 9.0+` | ````markdown<br/>{applies_to}`ece: ga 9.0+`<br/>```` |
| {applies_to}`eck: preview 9.1+` | ````markdown<br/>{applies_to}`eck: preview 9.1+`<br/>```` |
| {applies_to}`ece: deprecated 6.7+` | ````markdown<br/>{applies_to}`ece: deprecated 6.7+`<br/>```` |
| {applies_to}`ece: removed` | ````markdown<br/>{applies_to}`ece: removed`<br/>```` |
