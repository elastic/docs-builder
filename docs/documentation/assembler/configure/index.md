---
navigation_title: Configuration
---

# `assembler.yml`

The [`assembler.yml`](https://github.com/elastic/docs-builder/blob/main/config/assembler.yml) file defines the global documentation site:

* `environments`.
* `shared_configuration`.
* narrative repository configuration.
* reference repository configurations.

## `environments`

This section defines different build environments for the documentation site.

Each environment specifies configuration details such as the site URI, content source, path prefix, Google Tag Manager settings, and feature flags.

Example:

```yml
environments:
  prod:
    uri: https://www.elastic.co
    path_prefix: docs
    content_source: current
    allow_indexing: true
    google_tag_manager:
      enabled: true
      id: GTM-KNJMG2M
```

## `shared_configuration`

This section defines YAML anchors for common settings shared among multiple repositories and deployment environments.

The following example sets a unique `stack` version for each of the three defined deployment environments:

```yml
  stack: &stack
    current:  9.0
    next: 9.1
    edge: main
```

## `narrative`

Configures the main `docs-content` repository.

Example:

```yml
narrative:
  checkout_strategy: full
```

## `references`

Configures all other repositories whose docs content should be included or referenced in the build. Each can have custom settings for branch, checkout method, etc.

Example:

```yml
references:
  apm-server:
```

### Branching strategy

How you add a reference repository depends on its [branching strategy](../environments.md#branching-strategies).

#### Continuous deployment repository

To add a continuous deployment repository, define the name of the repository:

```yaml
references:
  my-repository:
```

The above configuration is equivalent to specifying. 

```yaml
references:
  my-repository:
    next: main
    current: main
```

### Tagged repository

To add a tagged repository, configure the repo to have a fixed git reference (typically a branch) deploy the `current` content source to production.

```yaml
references:
  my-other-repository:
    next: main
    current: 9.0
```

## Per-repository settings reference

The following settings can be specified on any entry under `narrative` or `references`:

| Key | Type | Default | Description |
|---|---|---|---|
| `current` | string | `main` | Git ref (branch, tag, or commit) used for the `current` content source. |
| `next` | string | `main` | Git ref used for the `next` content source. |
| `edge` | string | `main` | Git ref used for the `edge` content source. |
| `checkout_strategy` | `full` \| `partial` | `partial` | `full` clones the entire repository; `partial` uses a sparse checkout of `docs/` only. |
| `sparse_paths` | list of strings | `["docs"]` | Directories to include when `checkout_strategy: partial`. |
| `skip` | bool | `false` | Exclude this repository from the build. |
| `private` | bool | `false` | Mark repository as private; excluded from public builds when `--skip-private` is set. |
| `path` | string | — | Override the local filesystem path to use instead of cloning. Respected locally only (ignored in CI). |
| `clone_timeout` | duration | `10m` in CI | Per-attempt timeout for network git operations (fetch, pull) in CI. Accepts a positive integer followed by `s` (seconds) or `m` (minutes), e.g. `30s` or `15m`. Unbounded when not in CI. When omitted, the global default of 10 minutes applies. |

### `clone_timeout` example

```yaml
narrative:
  checkout_strategy: full
  clone_timeout: 15m   # large repo — give each fetch attempt extra headroom

references:
  my-fast-repo:
    clone_timeout: 30s   # small repo — fail quickly and retry
```