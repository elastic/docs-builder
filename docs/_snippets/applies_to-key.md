`applies_to` accepts the following keys in this structure:

* `stack`: Applies to the [Elastic Stack](https://www.elastic.co/docs/get-started/the-stack) including any Elastic Stack components.
* `deployment`: Applies to some deployment type(s).
  * `eck`: Applies to [Elastic Cloud on Kubernetes](https://www.elastic.co/docs/deploy-manage/deploy/cloud-on-k8s) deployments.
  * `ech`: Applies to [Elastic Cloud Hosted](https://www.elastic.co/docs/deploy-manage/deploy/elastic-cloud/cloud-hosted) deployments.
  * `ece`: Applies to [Elastic Cloud Enterprise](https://www.elastic.co/docs/deploy-manage/deploy/cloud-enterprise) deployments.
  * `self`: Applies to [self-managed](https://www.elastic.co/docs/deploy-manage/deploy/self-managed) deployments.
  * `ess`: Applies to the the Elasticsearch Service deployments. {applies_to}`product: deprecated`
    :::{note}
    Use `ech` instead.
    :::
* `serverless`: Applies to [Elastic Cloud Serverless](https://www.elastic.co/docs/deploy-manage/deploy/elastic-cloud/serverless).
  * `security`: Applies to Serverless [security projects](https://www.elastic.co/docs/solutions/security/get-started/create-security-project).
  * `elasticsearch`: Applies to Serverless [search projects](https://www.elastic.co/docs/solutions/search/serverless-elasticsearch-get-started).
  * `observability`: Applies to Serverless [observability projects](https://www.elastic.co/docs/solutions/observability/get-started).
* `product`: Applies to a specific product.
  * `ecctl`: Applies to [Elastic cloud control](https://www.elastic.co/docs/reference/ecctl).
  * `curator`: Applies to [Elasticsearch Curator](https://www.elastic.co/docs/reference/elasticsearch/curator).
  * `apm_agent_dotnet`: Applies to the [Elastic APM .NET agent](https://www.elastic.co/docs/reference/apm/agents/dotnet).
  * `apm_agent_go`: Applies to the [Elastic APM Go agent](https://www.elastic.co/docs/reference/apm/agents/go).
  * `apm_agent_java`: Applies to the [Elastic APM Java agent](https://www.elastic.co/docs/reference/apm/agents/java).
  * `apm_agent_node`: Applies to the [Elastic APM Node.js agent](https://www.elastic.co/docs/reference/apm/agents/nodejs).
  * `apm_agent_php`: Applies to the [Elastic APM PHP agent](https://www.elastic.co/docs/reference/apm/agents/php).
  * `apm_agent_python`: Applies to the [Elastic APM Python agent](https://www.elastic.co/docs/reference/apm/agents/python).
  * `apm_agent_ruby`: Applies to the [Elastic APM Ruby agent](https://www.elastic.co/docs/reference/apm/agents/ruby).
  * `apm_agent_rum`: Applies to the [APM RUM JavaScript agent](https://www.elastic.co/docs/reference/apm/agents/rum-js).
  * `edot_collector`: Applies to the [Elastic Distribution of OpenTelemetry Collector](https://www.elastic.co/docs/reference/opentelemetry/edot-collector/).
  * `edot_ios`: Applies to the [Elastic Distribution of OpenTelemetry iOS](https://www.elastic.co/docs/reference/opentelemetry/edot-sdks/ios/).
  * `edot_android`: Applies to the [Elastic Distribution of OpenTelemetry Android](https://www.elastic.co/docs/reference/opentelemetry/edot-sdks/android/).
  * `edot_dotnet`: Applies to the [Elastic Distribution of OpenTelemetry .NET](https://www.elastic.co/docs/reference/opentelemetry/edot-sdks/dotnet/).
  * `edot_java`: Applies to the [Elastic Distribution of OpenTelemetry Java](https://www.elastic.co/docs/reference/opentelemetry/edot-sdks/java/).
  * `edot_node`: Applies to the [Elastic Distribution of OpenTelemetry Node.js](https://www.elastic.co/docs/reference/opentelemetry/edot-sdks/nodejs/).
  * `edot_php`: Applies to the [Elastic Distribution of OpenTelemetry PHP](https://www.elastic.co/docs/reference/opentelemetry/edot-sdks/php/).
  * `edot_python`: Applies to the [Elastic Distribution of OpenTelemetry Python](https://www.elastic.co/docs/reference/opentelemetry/edot-sdks/python/).
  * `edot_cf_aws`: Applies to the [Elastic Distribution of OpenTelemetry Cloud Forwarder](https://www.elastic.co/docs/reference/opentelemetry/edot-cloud-forwarder/).
