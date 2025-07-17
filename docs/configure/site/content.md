# `assembler.yml`

The global navigation is defined in the [`assembler.yml`](https://github.com/elastic/docs-builder/blob/main/config/assembler.yml) file. This file can roughly be thought of as the V3 equivalent of conf.yaml in the asciidoc build system. This file, which writers own, allows for arbitrary nesting of `docset.yml` and `toc.yml` references. This file will live in the `elastic/docs-content` repository, but will not build or have any influence over the `docs-builder` builds in that repo.

The global navigation that is defined in [`assembler.yml`](https://github.com/elastic/docs-builder/blob/main/config/assembler.yml) can be composed of three main resources:
1. Local TOC files: toc.yml files that live in the docs-content repository.
2. External TOC files: A subset of a content set (represented by a toc.yml file) that is external to the docs-content repository.
3. External content set declarations: An entire docset.yml file that is external to the docs-content repository.

The [`assembler.yml`](https://github.com/elastic/docs-builder/blob/main/config/assembler.yml) file might look something like this:

```yaml

```

## Assembler constraints

To maintain each docset’s immutability and self-containment, resources in the global navigation (assembler.yml) must follow specific rules.
1. A link resource can appear only once in the global navigation, and it must nest directly under another resource (not under individual pages from a different content set). In other words:
    1. Each docset.yml or toc.yml file can appear only once in the overall navigation.
    1. Any TOC declarations—whether in docset.yml or toc.yml—must be placed either at the top level or directly under another TOC entry, with no individual files in between.
1. Nested resources must appear either before or after (default) the parent’s TOC, and they cannot be placed arbitrarily among the parent’s pages (e.g., between two files of the parent).
1. If an external TOC is linked, all of its child TOCs must also be included in the global navigation.

## AsciiDoctor conf.yml

In the AsciiDoctor-powered site, content configuration at the site level is done in the [`conf.yaml`](https://github.com/elastic/docs/blob/master/conf.yaml) file in the elastic/docs repo. In the `conf.yml` file, the configuration information for all books are listed in this one file. Here's the example of what it looks like to configure a single book:

```yaml
- title:      Machine Learning
  prefix:     en/machine-learning
  current:    *stackcurrent
  index:      docs/en/stack/ml/index.asciidoc
  branches:   [ {main: master}, 8.9, 8.8, 8.7, 8.6, 8.5, 8.4, 8.3, 8.2, 8.1, 8.0, 7.17, 7.16, 7.15, 7.14, 7.13, 7.12, 7.11, 7.10, 7.9, 7.8, 7.7, 7.6, 7.5, 7.4, 7.3, 7.2, 7.1, 7.0, 6.8, 6.7, 6.6, 6.5, 6.4, 6.3 ]
  live:       *stacklive
  chunk:      1
  tags:       Elastic Stack/Machine Learning
  subject:    Machine Learning
  sources:
    -
      repo:   stack-docs
      path:   docs/en/stack
    -
      repo:   elasticsearch
      path:   docs
    -
      repo:   docs
      path:   shared/versions/stack/{version}.asciidoc
    -
      repo:   docs
      path:   shared/attributes.asciidoc
    -
      repo:   docs
      path:   shared/settings.asciidoc
```
