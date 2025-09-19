`products` is a list of objects, each object has an `id` field.

```yaml
products:
  - id: apm
```

Each product ID is mapped to a full product name.
You use the product ID in the source files, and docs-builder will use the full product name in
a `product_name` `meta` tag in the generated HTML and YAML metadata in generated Markdown files,
which are used to drive elastic.co search functionality.

:::{dropdown} Valid products IDs

| Product ID                                  | Product name                                  |
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
| `edot-cf`                                   | EDOT Cloud Forwarder                          |
| `edot-sdk`                                  | Elastic Distribution of OpenTelemetry SDK     |
| `edot-collector`                            | Elastic Distribution of OpenTelemetry Collector |
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
:::
