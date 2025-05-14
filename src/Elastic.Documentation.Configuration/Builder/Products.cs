// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;

namespace Elastic.Documentation.Configuration.Builder;

public record Product(string Id, string DisplayName);

public static class Products
{
	public static FrozenSet<Product> All { get; } = [
		new("apm-agent", "APM Agent"),
		new("apm", "APM"),
		new("auditbeat", "Auditbeat"),
		new("beats", "Beats"),
		new("cloud-control-ecctl", "Elastic Cloud Control ECCTL"),
		new("cloud-enterprise", "Elastic Cloud Enterprise"),
		new("cloud-hosted", "Elastic Cloud Hosted"),
		new("cloud-kubernetes", "Elastic Cloud Kubernetes"),
		new("cloud-serverless", "Elastic Cloud Serverless"),
		new("cloud-terraform", "Elastic Cloud Terraform"),
		new("ecs-logging", "ECS Logging"),
		new("ecs", "Elastic Common Schema (ECS)"),
		new("edot-sdk", "Elastic Distribution of OpenTelemetry SDK"),
		new("edot-collector", "Elastic Distribution of OpenTelemetry Collector"),
		new("elastic-agent", "Elastic Agent"),
		new("elastic-products-platform", "Elastic Products platform"),
		new("elastic-serverless-forwarder", "Elastic Serverless Forwarder"),
		new("elastic-stack", "Elastic Stack"),
		new("elasticsearch-community-clients", "Elasticsearch community clients"),
		new("elasticsearch-curator", "Elasticsearch Curator"),
		new("elasticsearch-client", "Elasticsearch Client"),
		new("painless", "Elasticsearch Painless scripting language"),
		new("elasticsearch", "Elasticsearch"),
		new("filebeat", "Filebeat"),
		new("fleet", "Fleet"),
		new("heartbeat", "Heartbeat"),
		new("integrations", "Integrations"),
		new("kibana", "Kibana"),
		new("logstash", "Logstash"),
		new("machine-learning", "Machine Learning"),
		new("metricbeat", "Metricbeat"),
		new("observability", "Elastic Observability"),
		new("packetbeat", "Packetbeat"),
		new("search-ui", "Search UI"),
		new("security", "Elastic Security"),
		new("winlogbeat", "Winlogbeat"),
	];

	public static FrozenDictionary<string, Product> AllById { get; } = All.ToDictionary(p => p.Id, StringComparer.Ordinal).ToFrozenDictionary();
}
