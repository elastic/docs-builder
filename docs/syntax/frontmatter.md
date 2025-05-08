# Frontmatter

Every Markdown file referenced in the TOC may optionally define a frontmatter block.
Frontmatter is YAML-formatted metadata about a page, at the beginning of each file
and wrapped by `---` lines.

In the frontmatter block, you can define the following fields:

```yaml
---
navigation_title: This is the navigation title <1>
description: This is a description of the page <2>
applies_to: <3>
  serverless: all
products: <4>
  - apm-java-agent
  - edot-java
---
```

1. [`navigation_title`](#navigation-title)
2. [`description`](#description)
3. [`applies_to`](#applies-to)
4. [`products`](#products)

## Navigation Title

See [](./titles.md)

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

The products frontmatter is a list of strings, each string is the id of a product.

| Product ID                                  | Product Name                                  |
|---------------------------------------------|-----------------------------------------------|
| `apm`                                       | APM                                           |
| `apm-android-agent`                         | APM Android Agent                             |
| `apm-attacher`                              | APM Attacher                                  |
| `apm-aws-lambda-extension`                  | APM AWS Lambda extension                      |
| `apm-dotnet-agent`                          | APM .NET Agent                                |
| `apm-go-agent`                              | APM Go Agent                                  |
| `apm-ios-agent`                             | APM iOS Agent                                 |
| `apm-java-agent`                            | APM Java Agent                                |
| `apm-node-agent`                            | APM Node.js Agent                             |
| `apm-php-agent`                             | APM PHP Agent                                 |
| `apm-python-agent`                          | APM Python Agent                              |
| `apm-ruby-agent`                            | APM Ruby Agent                                |
| `apm-rum-agent`                             | APM RUM Agent                                 |
| `beats-logging-plugin`                      | Beats Logging plugin                          |
| `cloud-control-ecctl`                       | Cloud Control ECCTL                           |
| `cloud-enterprise`                          | Cloud Enterprise                              |
| `cloud-hosted`                              | Cloud Hosted                                  |
| `cloud-kubernetes`                          | Cloud Kubernetes                              |
| `cloud-native-ingest`                       | Cloud Native Ingest                           |
| `cloud-serverless`                          | Cloud Serverless                              |
| `cloud-terraform`                           | Cloud Terraform                               |
| `ecs`                                       | Elastic Common Schema (ECS)                   |
| `ecs-logging-dotnet`                        | ECS Logging .NET                              |
| `ecs-logging-go-logrus`                     | ECS Logging Go Logrus                         |
| `ecs-logging-go-zap`                        | ECS Logging Go Zap                            |
| `ecs-logging-go-zerolog`                    | ECS Logging Go Zerolog                        |
| `ecs-logging-java`                          | ECS Logging Java                              |
| `ecs-logging-node`                          | ECS Logging Node.js                           |
| `ecs-logging-php`                           | ECS Logging PHP                               |
| `ecs-logging-python`                        | ECS Logging Python                            |
| `ecs-logging-ruby`                          | ECS Logging Ruby                              |
| `edot-android`                              | Elastic Distribution of OpenTelemetry Android |
| `edot-collector`                            | Elastic Distribution of OpenTelemetry Collector |
| `edot-dotnet`                               | Elastic Distribution of OpenTelemetry .NET     |
| `edot-ios`                                  | Elastic Distribution of OpenTelemetry iOS     |
| `edot-java`                                 | Elastic Distribution of OpenTelemetry Java     |
| `edot-nodejs`                               | Elastic Distribution of OpenTelemetry Node.js   |
| `edot-php`                                  | Elastic Distribution of OpenTelemetry PHP     |
| `edot-python`                               | Elastic Distribution of OpenTelemetry Python   |
| `elastic-agent`                             | Elastic Agent                                 |
| `elastic-products-platform`                 | Elastic Products platform                     |
| `elastic-stack`                             | Elastic Stack                                 |
| `elasticsearch`                             | Elasticsearch                                 |
| `elasticsearch-apache-hadoop`               | Elasticsearch Apache Hadoop                   |
| `elasticsearch-cloud-hosted-heroku`         | Elasticsearch Cloud Hosted Heroku             |
| `elasticsearch-community-clients`           | Elasticsearch community clients               |
| `elasticsearch-curator`                     | Elasticsearch Curator                         |
| `elasticsearch-dotnet-client`               | Elasticsearch .NET Client                     |
| `elasticsearch-eland-python-client`         | Elasticsearch Eland Python Client             |
| `elasticsearch-go-client`                   | Elasticsearch Go Client                       |
| `elasticsearch-groovy-client`               | Elasticsearch Groovy Client                   |
| `elasticsearch-java-client`                 | Elasticsearch Java Client                     |
| `elasticsearch-java-script-client`          | Elasticsearch JavaScript Client               |
| `elasticsearch-painless-scripting-language` | Elasticsearch Painless scripting language     |
| `elasticsearch-perl-client`                 | Elasticsearch Perl Client                     |
| `elasticsearch-php-client`                  | Elasticsearch PHP Client                      |
| `elasticsearch-plugins`                     | Elasticsearch plugins                         |
| `elasticsearch-python-client`               | Elasticsearch Python Client                   |
| `elasticsearch-resiliency-status`           | Elasticsearch Resiliency Status               |
| `elasticsearch-ruby-client`                 | Elasticsearch Ruby Client                     |
| `elasticsearch-rust-client`                 | Elasticsearch Rust Client                     |
| `fleet`                                     | Fleet                                         |
| `ingest`                                    | Ingest                                        |
| `integrations`                              | Integrations                                  |
| `kibana`                                    | Kibana                                        |
| `logstash`                                  | Logstash                                      |
| `machine-learning`                          | Machine Learning                              |
| `observability`                             | Observability                                 |
| `reference-architectures`                   | Reference Architectures                       |
| `search-ui`                                 | Search UI                                     |
| `security`                                  | Security                                      |
