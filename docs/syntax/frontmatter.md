# Frontmatter

Every Markdown file referenced in the TOC may optionally define a frontmatter block.
Frontmatter is YAML-formatted metadata about a page, at the beginning of each file
and wrapped by `---` lines.

In the frontmatter block, you can define the following fields:

```yaml
---
navigation_title: This is the navigation title. <1>
meta_title: This is the page title for search results and browser tabs. <2>
description: This is a description of the page. <3>
applies_to: <4>
  serverless: all
products: <5>
  - id: apm-agent
  - id: edot-sdk
sub: <6>
  key: value 
cta: <7>
  id: beta
---
```

1. [`navigation_title`](#navigation-title)
2. [`meta_title`](#meta-title)
3. [`description`](#description)
4. [`applies_to`](#applies-to)
5. [`products`](#products)
6. [`sub`](#subs)
7. [`cta`](#cta)

## Navigation Title

See [](./titles.md)

## Meta title

Use `meta_title` to set the page title that appears in browser tabs and search results.
By default, the page title comes from the level-one heading.
The visible heading, the navigation title, and the indexed title stay the same.

```yaml
---
meta_title: Search across clusters
---

# Cross-cluster search
```

The builder appends the site name to the title. Do not include it in `meta_title`.
Substitutions and Markdown formatting are resolved before the title is used.
Authored Markdown pages in API reference sections support `meta_title` too.

## Description

Use the `description` frontmatter to set the description meta tag for a page.
This helps search engines and social media.
It also sets the `og:description` and `twitter:description` meta tags.

The `description` frontmatter is a string, recommended to be around 150 characters. If you don't set a `description`,
it will be generated from the first few paragraphs of the page until it reaches 150 characters.

## Applies to

See [](./applies.md)

## Products

The products frontmatter is a list of products that the page relates to.
This is used for the "Products" filter in the Search UI.

The products frontmatter is a list of objects, each object has an `id` field.
Only products with the `public-reference` feature enabled in [`products.yml`](https://github.com/elastic/docs-builder/blob/main/config/products.yml) are valid here. Products that have set `public-reference: false` cannot be used in frontmatter.

| Product ID                                  | Product Name                                  |
|---------------------------------------------|-----------------------------------------------|
| `apm`                                       | APM                                           |
| `apm-agent`                                 | APM Agent                                     |
| `auditbeat`                                 | Auditbeat                                     |
| `beats`                                     | Beats                                         |
| `cloud-control-ecctl`                       | Elastic Cloud Control ECCTL                   |
| `cloud-enterprise`                          | Elastic Cloud Enterprise                      |
| `cloud-hosted`                              | Elastic Cloud Hosted                          |
| `cloud-kubernetes`                          | Elastic Cloud Kubernetes                      |
| `cloud-serverless`                          | Elastic Cloud Serverless                      |
| `cloud-terraform`                           | Elastic Cloud Terraform                       |
| `ecs`                                       | Elastic Common Schema (ECS)                   |
| `ecs-logging`                               | ECS Logging                                   |
| `edot-cf`                                   | Elastic Cloud Forwarder                       |
| `edot-sdk`                                  | Elastic Distribution of OpenTelemetry SDK     |
| `edot-collector`                            | Elastic Agent                                 |
| `elastic-agent`                             | Elastic Agent                                 |
| `elastic-serverless-forwarder`              | Elastic Serverless Forwarder                  |
| `elastic-stack`                             | Elastic Stack                                 |
| `elasticsearch`                             | Elasticsearch                                 |
| `elasticsearch-client`                      | Elasticsearch Client                          |
| `filebeat`                                  | Filebeat                                      |
| `fleet`                                     | Fleet                                         |
| `heartbeat`                                 | Heartbeat                                     |
| `integrations`                              | Integrations                                  |
| `kibana`                                    | Kibana                                        |
| `logstash`                                  | Logstash                                      |
| `machine-learning`                          | Machine Learning                              |
| `metricbeat`                                | Metricbeat                                    |
| `observability`                             | Elastic Observability                         |
| `packetbeat`                                | Packetbeat                                    |
| `painless`                                  | Painless                                      |
| `search-ui`                                 | Search UI                                     |
| `security`                                  | Elastic Security                              |
| `winlogbeat`                                | Winlogbeat                                    |

## Subs

Use the `sub` field to define local substitutions. Refer to [Substitutions](substitutions.md) for more information.

## CTA

See [CTA](../documentation/isolated/cta.md).