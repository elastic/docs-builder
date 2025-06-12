// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;

namespace Elastic.Documentation.Configuration.Builder;

public record Product(string Id, string DisplayName, HashSet<Product>? SubProduct = null);

public static class Products
{
	public static FrozenSet<Product> All { get; } = [
		new("apm", "APM"),
		new("apm-agent", "APM Agent", [
			new Product("apm-agent-android", "APM Android Agent"),
			new Product("apm-agent-dotnet", "APM .NET Agent"),
			new Product("apm-agent-go", "APM Go Agent"),
			new Product("apm-agent-java", "APM Java Agent"),
			new Product("apm-agent-nodejs", "APM Node.js Agent"),
			new Product("apm-agent-php", "APM PHP Agent"),
			new Product("apm-agent-python", "APM Python Agent"),
			new Product("apm-agent-ruby", "APM Ruby Agent"),
			new Product("apm-agent-rum-js", "APM RUM JS Agent"),
			new Product("apm-agent-ios", "APM RUM React Agent"),
		]),
		new("auditbeat", "Auditbeat"),
		new("beats", "Beats"),
		new("cloud-control-ecctl", "Elastic Cloud Control ECCTL"),
		new("cloud-enterprise", "Elastic Cloud Enterprise"),
		new("cloud-hosted", "Elastic Cloud Hosted"),
		new("cloud-kubernetes", "Elastic Cloud Kubernetes"),
		new("cloud-serverless", "Elastic Cloud Serverless"),
		new("cloud-terraform", "Elastic Cloud Terraform"),
		new("ecs", "Elastic Common Schema (ECS)"),
		new("ecs-logging", "ECS Logging"),
		new("edot-sdk", "Elastic Distribution of OpenTelemetry SDK"),
		new("edot-collector", "Elastic Distribution of OpenTelemetry Collector"),
		new("elastic-agent", "Elastic Agent"),
		new("elastic-serverless-forwarder", "Elastic Serverless Forwarder"),
		new("elastic-stack", "Elastic Stack"),
		new("elasticsearch", "Elasticsearch"),
		new("elasticsearch-client", "Elasticsearch Client"),
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
		new("painless", "Elasticsearch Painless scripting language"),
		new("search-ui", "Search UI"),
		new("security", "Elastic Security"),
		new("winlogbeat", "Winlogbeat"),
	];

	public static FrozenDictionary<string, Product> AllById { get; } = All.ToDictionary(p => p.Id, StringComparer.Ordinal).ToFrozenDictionary();
}
