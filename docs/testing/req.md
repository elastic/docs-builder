---
applies_to:
  stack: ga
  serverless:
    elasticsearch: ga
mapped_pages:
  - https://www.elastic.co/guide/en/elasticsearch/reference/current/es-connectors-usage.html
---
# Requirements

This page demonstrates various `applies_to` version syntax examples.

## Version specifier examples

### Greater than or equal (default)

```{applies_to}
stack: ga 9.0
```

This is equivalent to `ga 9.0+` — the feature is available from version 9.0 onwards.

### Explicit range

```{applies_to}
stack: beta 9.0-9.1, ga 9.2
```

The feature was in beta from 9.0 to 9.1 (inclusive), then became GA in 9.2+.

### Exact version

```{applies_to}
stack: preview =9.0, ga 9.1
```

The feature was in preview only in version 9.0 (exactly), then became GA in 9.1+.

## Implicit version inference examples

### Simple two-stage lifecycle

```{applies_to}
stack: preview 9.0, ga 9.1
```

Interpreted as: `preview =9.0` (exact), `ga 9.1+` (open-ended).

### Multi-stage lifecycle with consecutive versions

```{applies_to}
stack: preview 9.0, beta 9.1, ga 9.2
```

Interpreted as: `preview =9.0`, `beta =9.1`, `ga 9.2+`.

### Multi-stage lifecycle with gaps

```{applies_to}
stack: unavailable 9.0, beta 9.1, preview 9.2, ga 9.4
```

Interpreted as: `unavailable =9.0`, `beta =9.1`, `preview 9.2-9.3` (range to fill the gap), `ga 9.4+`.

### Three stages with varying gaps

```{applies_to}
stack: preview 8.0, beta 9.1, ga 9.3
```

Interpreted as: `preview 8.0-8.19`, `beta 9.0-9.1`, `ga 9.2+`.

## Inline examples

{applies_to}`stack: preview 9.0` This feature is in preview in 9.0.

{applies_to}`stack: beta 9.0-9.1` This feature was in beta from 9.0 to 9.1.

{applies_to}`stack: ga 9.2+` This feature is generally available since 9.2.

{applies_to}`stack: preview =9.0` This feature was in preview only in 9.0 (exact).

## Explicit patch version display

By default, patch versions (e.g., the `.4` in `9.0.4`) are hidden in badge displays, showing only `9.0` instead. To explicitly show the patch version, add an exclamation mark `!` after the version declaration.

### Single version with explicit patch

{applies_to}`stack: preview 7.5.4!` Shows `7.5.4+` instead of `7.5+`.

{applies_to}`stack: preview 7.5.4` Without `!`, shows `7.5+` (patch hidden).

### Range versions with explicit patch

{applies_to}`stack: beta 7.0.3!-7.5.2!` Shows `7.0.3-7.5.2` with both patch versions visible.

{applies_to}`stack: ga 7.0.5!-7.5` Shows `7.0.5-7.5` with patch only on min.

{applies_to}`stack: ga 7.0-7.5.3!` Shows `7.0-7.5.3` with patch only on max.

### Exact version with explicit patch

{applies_to}`stack: ga =7.5.3!` Shows `7.5.3` as an exact version with patch visible.

## Deprecation and removal examples

```{applies_to}
stack: deprecated 9.2, removed 9.5
```

Interpreted as: `deprecated 9.2-9.4`, `removed 9.5+`.

{applies_to}`stack: deprecated 9.0` This feature is deprecated starting in 9.0.

{applies_to}`stack: removed 9.2` This feature was removed in 9.2.

## Mixed deployment examples

```{applies_to}
stack: ga 9.0
deployment:
  ece: ga 4.0
  eck: beta 3.0, ga 3.1
```

### Handling multiple future versions

```{applies_to}
eck: beta 3.4, ga 3.5, deprecated 3.9
```


## Additional content

To follow this tutorial you will need to install the following components:

- An installation of Elasticsearch, based on our hosted [Elastic Cloud](https://www.elastic.co/cloud) service (which includes a free trial period), or a self-hosted service that you run on your own computer. See the Install Elasticsearch section above for installation instructions.
- A [Python](https://python.org) interpreter. Make sure it is a recent version, such as Python 3.8 or newer.

The tutorial assumes that you have no previous knowledge of Elasticsearch or general search topics, but it expects you to have a basic familiarity with the following technologies, at least at a beginner level:

- Python development
- The [Flask](https://flask.palletsprojects.com/) web framework for Python.
- The command prompt or terminal application in your operating system.

{applies_to}`ece: removed`

{applies_to}`ece: `

{applies_to}`stack: deprecated 7.16.0, removed 8.0.0`

{applies_to}`ess: `

{applies_to}`stack: preview 9.0, ga 9.2, deprecated 9.7`

{applies_to}`stack: preview 9.0, removed 9.1`

{applies_to}`stack: preview 9.0.0-9.0.3, removed 9.3`

{applies_to}`stack: preview 9.0, ga 9.4, removed 9.7`

{applies_to}`stack: preview 9.0, deprecated 9.4, removed 9.7`
