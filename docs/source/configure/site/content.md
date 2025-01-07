---
title: Content config
---

In both the AsciiDoctor- and V3-based system, there is site-wide configuration where you list all content sources, where to find those sources, and in what order they should be added to the site.

In the AsciiDoctor system, this all happens in one YAML file in the /docs repo. In the V3 system, this ...

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

## V3 configuration

TO DO