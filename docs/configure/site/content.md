---
navigation_title: assembler.yml
---

# `assembler.yml`

The [`assembler.yml`](https://github.com/elastic/docs-builder/blob/main/config/assembler.yml) file defines the global documentation site:

* `environments`
* `shared_configuration`
* narrative repository configuration
* reference repository configurations

## `environments`

This section defines different build environments for the documentation site.

Each environment specifies configuration details such as the site URI, content source, path prefix, Google Tag Manager settings, and feature flags.

## `shared_configuration`

This section defines YAML anchors for common settings shared among multiple repositories.

## `narrative`

Configures the main `docs-content` repository.

## `references`

Configures all other repositories whose docs content should be included or referenced in the build. Each can have custom settings for branch, checkout method, etc.

### Branching strategy

How you add a reference repository depends on its [branching strategy](../content-sources.md#branching-strategies).

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