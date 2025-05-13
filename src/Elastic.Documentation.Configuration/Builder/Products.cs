// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;

namespace Elastic.Documentation.Configuration.Builder;

public record Product(string Id, string DisplayName);

public static class Products
{
	public static FrozenSet<Product> All { get; } = [
		new("apm-android-agent", "APM Android Agent"),
		new("apm-attacher", "APM Attacher"),
		new("apm-aws-lambda-extension", "APM AWS Lambda extension"),
		new("apm-dotnet-agent", "APM .NET Agent"),
		new("apm-go-agent", "APM Go Agent"),
		new("apm-ios-agent", "APM iOS Agent"),
		new("apm-java-agent", "APM Java Agent"),
		new("apm-node-agent", "APM Node.js Agent"),
		new("apm-php-agent", "APM PHP Agent"),
		new("apm-python-agent", "APM Python Agent"),
		new("apm-ruby-agent", "APM Ruby Agent"),
		new("apm-rum-agent", "APM RUM Agent"),
		new("apm", "APM"),
		new("auditbeat","Auditbeat"),
		new("beats-logging-plugin", "Beats Logging plugin"),
		new("beats","Beats"),
		new("cloud-control-ecctl", "Cloud Control ECCTL"),
		new("cloud-enterprise", "Cloud Enterprise"),
		new("cloud-hosted", "Cloud Hosted"),
		new("cloud-kubernetes", "Cloud Kubernetes"),
		new("cloud-serverless", "Cloud Serverless"),
		new("cloud-terraform", "Cloud Terraform"),
		new("ecs-logging-dotnet", "ECS Logging .NET"),
		new("ecs-logging-go-logrus", "ECS Logging Go Logrus"),
		new("ecs-logging-go-zap", "ECS Logging Go Zap"),
		new("ecs-logging-go-zerolog", "ECS Logging Go Zerolog"),
		new("ecs-logging-java", "ECS Logging Java"),
		new("ecs-logging-node", "ECS Logging Node.js"),
		new("ecs-logging-php", "ECS Logging PHP"),
		new("ecs-logging-python", "ECS Logging Python"),
		new("ecs-logging-ruby", "ECS Logging Ruby"),
		new("ecs-logging", "ECS Logging"),
		new("ecs", "Elastic Common Schema (ECS)"),
		new("edot-android", "Elastic Distribution of OpenTelemetry Android"),
		new("edot-collector", "Elastic Distribution of OpenTelemetry Collector"),
		new("edot-dotnet", "Elastic Distribution of OpenTelemetry .NET"),
		new("edot-ios", "Elastic Distribution of OpenTelemetry iOS"),
		new("edot-java", "Elastic Distribution of OpenTelemetry Java"),
		new("edot-nodejs", "Elastic Distribution of OpenTelemetry Node.js"),
		new("edot-php", "Elastic Distribution of OpenTelemetry PHP"),
		new("edot-python", "Elastic Distribution of OpenTelemetry Python"),
		new("elastic-agent", "Elastic Agent"),
		new("elastic-products-platform", "Elastic Products platform"),
		new("elastic-stack", "Elastic Stack"),
		new("elastic-stack","Elastic Stack"),
		new("elasticsearch-apache-hadoop", "Elasticsearch Apache Hadoop"),
		new("elasticsearch-community-clients", "Elasticsearch community clients"),
		new("elasticsearch-curator", "Elasticsearch Curator"),
		new("elasticsearch-dotnet-client", "Elasticsearch .NET Client"),
		new("elasticsearch-eland-python-client", "Elasticsearch Eland Python Client"),
		new("elasticsearch-go-client", "Elasticsearch Go Client"),
		new("elasticsearch-groovy-client", "Elasticsearch Groovy Client"),
		new("elasticsearch-java-client", "Elasticsearch Java Client"),
		new("elasticsearch-java-script-client", "Elasticsearch JavaScript Client"),
		new("elasticsearch-painless-scripting-language", "Elasticsearch Painless scripting language"),
		new("elasticsearch-perl-client", "Elasticsearch Perl Client"),
		new("elasticsearch-php-client", "Elasticsearch PHP Client"),
		new("elasticsearch-plugins", "Elasticsearch plugins"),
		new("elasticsearch-python-client", "Elasticsearch Python Client"),
		new("elasticsearch-resiliency-status", "Elasticsearch Resiliency Status"),
		new("elasticsearch-ruby-client", "Elasticsearch Ruby Client"),
		new("elasticsearch-rust-client", "Elasticsearch Rust Client"),
		new("elasticsearch", "Elasticsearch"),
		new("filebeat","Filebeat"),
		new("fleet", "Fleet"),
		new("heartbeat","Heartbeat"),
		new("integrations", "Integrations"),
		new("integrations","Integrations"),
		new("kibana", "Kibana"),
		new("logstash", "Logstash"),
		new("machine-learning", "Machine Learning"),
		new("metricbeat","Metricbeat"),
		new("observability", "Observability"),
		new("packetbeat","Packetbeat"),
		new("search-ui", "Search UI"),
		new("security", "Security"),
		new("winlogbeat","Winlogbeat")
	];

	public static FrozenDictionary<string, Product> AllById { get; } = All.ToDictionary(p => p.Id, StringComparer.Ordinal).ToFrozenDictionary();
}
