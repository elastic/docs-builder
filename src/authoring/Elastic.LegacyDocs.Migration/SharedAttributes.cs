// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.LegacyDocs.Migration;

/// <summary>
/// Well-known AsciiDoc attributes from elastic/docs shared/attributes.asciidoc
/// that map to simple display strings (not URLs).
/// These are emitted as subs in the generated docset.yml.
/// </summary>
public static class SharedAttributes
{
	public static readonly Dictionary<string, string> ProductNames = new(StringComparer.OrdinalIgnoreCase)
	{
		// Cloud
		["ecloud"] = "Elastic Cloud",
		["ess"] = "Elasticsearch Service",
		["ech"] = "Elastic Cloud Hosted",
		["ece"] = "Elastic Cloud Enterprise",
		["eck"] = "Elastic Cloud on Kubernetes",
		["esf"] = "Elastic Serverless Forwarder",
		["serverless-full"] = "Elastic Cloud Serverless",
		["serverless-short"] = "Serverless",
		["es-serverless"] = "Elasticsearch Serverless",
		["obs-serverless"] = "Elastic Observability Serverless",
		["sec-serverless"] = "Elastic Security Serverless",
		// Core products
		["es"] = "Elasticsearch",
		["kib"] = "Kibana",
		["ls"] = "Logstash",
		["beats"] = "Beats",
		["stack"] = "Elastic Stack",
		["xpack"] = "X-Pack",
		["es-sql"] = "Elasticsearch SQL",
		["esql"] = "ES|QL",
		// Beats
		["auditbeat"] = "Auditbeat",
		["filebeat"] = "Filebeat",
		["heartbeat"] = "Heartbeat",
		["metricbeat"] = "Metricbeat",
		["packetbeat"] = "Packetbeat",
		["winlogbeat"] = "Winlogbeat",
		["functionbeat"] = "Functionbeat",
		["journalbeat"] = "Journalbeat",
		// Agents and ingest
		["agent"] = "Elastic Agent",
		["agents"] = "Elastic Agents",
		["fleet"] = "Fleet",
		["fleet-server"] = "Fleet Server",
		["integrations-server"] = "Integrations Server",
		["integrations"] = "Integrations",
		// Enterprise Search
		["ents"] = "Enterprise Search",
		["crawler"] = "Enterprise Search web crawler",
		// Observability
		["observability"] = "Observability",
		// Security
		["elastic-sec"] = "Elastic Security",
		["elastic-defend"] = "Elastic Defend",
		["elastic-endpoint"] = "Elastic Endpoint",
		["endpoint-sec"] = "Endpoint Security",
		// ML
		["ml"] = "machine learning",
		["ml-cap"] = "Machine learning",
		["ml-init"] = "ML",
		["nlp"] = "natural language processing",
		["nlp-cap"] = "Natural language processing",
		// Features
		["security"] = "X-Pack security",
		["security-features"] = "security features",
		["monitor-features"] = "monitoring features",
		["ml-features"] = "machine learning features",
		["alert-features"] = "alerting features",
		["report-features"] = "reporting features",
		["graph-features"] = "graph analytics features",
		["watcher"] = "Watcher",
		["monitoring"] = "X-Pack monitoring",
		["reporting"] = "X-Pack reporting",
		["graph"] = "X-Pack graph",
		// Abbreviations
		["ccr"] = "cross-cluster replication",
		["ccr-cap"] = "Cross-cluster replication",
		["ccr-init"] = "CCR",
		["ccs"] = "cross-cluster search",
		["ccs-cap"] = "Cross-cluster search",
		["ccs-init"] = "CCS",
		["ilm"] = "index lifecycle management",
		["ilm-cap"] = "Index lifecycle management",
		["ilm-init"] = "ILM",
		["slm"] = "snapshot lifecycle management",
		["slm-cap"] = "Snapshot lifecycle management",
		["slm-init"] = "SLM",
		["rollup"] = "rollup",
		["rollup-cap"] = "Rollup",
		["transform"] = "transform",
		["transform-cap"] = "Transform",
		["transforms"] = "transforms",
		["transforms-cap"] = "Transforms",
		["dfeed"] = "datafeed",
		["dfeeds"] = "datafeeds",
		["anomaly-detect"] = "anomaly detection",
		["anomaly-detect-cap"] = "Anomaly detection",
		["infer"] = "inference",
		["infer-cap"] = "Inference",
		["search-snaps"] = "searchable snapshots",
		["search-snaps-cap"] = "Searchable snapshots",
		// Data views
		["data-source"] = "data view",
		["data-sources"] = "data views",
		["data-source-cap"] = "Data view",
		["data-sources-cap"] = "Data views",
		// Kibana apps
		["apm-app"] = "APM app",
		["uptime-app"] = "Uptime app",
		["synthetics-app"] = "Synthetics app",
		["logs-app"] = "Logs app",
		["metrics-app"] = "Metrics app",
		["infrastructure-app"] = "Infrastructure app",
		["security-app"] = "Elastic Security app",
		["ml-app"] = "Machine Learning",
		["dev-tools-app"] = "Dev Tools",
		["stack-manage-app"] = "Stack Management",
		["stack-monitor-app"] = "Stack Monitoring",
		["maps-app"] = "Maps",
		["data-views-app"] = "Data Views",
		// APM agents
		["apm-agent"] = "APM agent",
		["apm-go-agent"] = "Elastic APM Go agent",
		["apm-java-agent"] = "Elastic APM Java agent",
		["apm-dotnet-agent"] = "Elastic APM .NET agent",
		["apm-node-agent"] = "Elastic APM Node.js agent",
		["apm-php-agent"] = "Elastic APM PHP agent",
		["apm-py-agent"] = "Elastic APM Python agent",
		["apm-ruby-agent"] = "Elastic APM Ruby agent",
		["apm-rum-agent"] = "Elastic APM Real User Monitoring (RUM) JavaScript agent",
		// Misc
		["k8s"] = "Kubernetes",
		["aws"] = "AWS",
		["esh"] = "ES-Hadoop",
		["searchprofiler"] = "Search Profiler",
		["data-viz"] = "Data Visualizer",
		["feat-imp"] = "feature importance",
		["feat-imp-cap"] = "Feature importance",
		// Connectors
		["sn"] = "ServiceNow",
		["sn-itsm"] = "ServiceNow ITSM",
		["sn-itom"] = "ServiceNow ITOM",
		["sn-sir"] = "ServiceNow SecOps",
		["jira"] = "Jira",
		["swimlane"] = "Swimlane",
		["opsgenie"] = "Opsgenie",
		["bedrock"] = "Amazon Bedrock",
		["gemini"] = "Google Gemini",
	};
}
