// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Markdown.Myst.Components;

public static class ApplicabilityMappings
{
	public record ApplicabilityDefinition(string Key, string DisplayName, VersioningSystemId VersioningSystemId);

	private static readonly Dictionary<string, ApplicabilityDefinition> Mappings = new()
	{
		// Stack
		["stack"] = new ApplicabilityDefinition("Stack", "Elastic&nbsp;Stack", VersioningSystemId.Stack),

		// Serverless
		["serverless"] = new ApplicabilityDefinition("Serverless", "Elastic&nbsp;Cloud&nbsp;Serverless", VersioningSystemId.Serverless),
		["serverless-elasticsearch"] = new ApplicabilityDefinition("Serverless Elasticsearch", "Serverless&nbsp;Elasticsearch projects", VersioningSystemId.ElasticsearchProject),
		["serverless-observability"] = new ApplicabilityDefinition("Serverless Observability", "Serverless&nbsp;Observability projects", VersioningSystemId.ObservabilityProject),
		["serverless-security"] = new ApplicabilityDefinition("Serverless Security", "Serverless&nbsp;Security projects", VersioningSystemId.SecurityProject),

		// Deployment
		["ech"] = new ApplicabilityDefinition("ECH", "Elastic&nbsp;Cloud&nbsp;Hosted", VersioningSystemId.Ess),
		["eck"] = new ApplicabilityDefinition("ECK", "Elastic&nbsp;Cloud&nbsp;on&nbsp;Kubernetes", VersioningSystemId.Eck),
		["ece"] = new ApplicabilityDefinition("ECE", "Elastic&nbsp;Cloud&nbsp;Enterprise", VersioningSystemId.Ece),
		["self"] = new ApplicabilityDefinition("Self-Managed", "Self-managed Elastic&nbsp;deployments", VersioningSystemId.Self),

		// Product Applicability
		["ecctl"] = new ApplicabilityDefinition("ECCTL", "Elastic&nbsp;Cloud&nbsp;Control", VersioningSystemId.Ecctl),
		["curator"] = new ApplicabilityDefinition("Curator", "Curator", VersioningSystemId.Curator),

		// EDOT Products
		["edot-android"] = new ApplicabilityDefinition("EDOT Android", "Elastic&nbsp;Distribution of OpenTelemetry&nbsp;Android", VersioningSystemId.EdotAndroid),
		["edot-cf-aws"] = new ApplicabilityDefinition("EDOT CF AWS", "Elastic&nbsp;Distribution of OpenTelemetry&nbsp;Cloud&nbsp;Forwarder for AWS", VersioningSystemId.EdotCfAws),
		["edot-collector"] = new ApplicabilityDefinition("EDOT Collector", "Elastic Distribution of OpenTelemetry Collector", VersioningSystemId.EdotCollector),
		["edot-dotnet"] = new ApplicabilityDefinition("EDOT .NET", "Elastic&nbsp;Distribution of OpenTelemetry&nbsp;.NET", VersioningSystemId.EdotDotnet),
		["edot-ios"] = new ApplicabilityDefinition("EDOT iOS", "Elastic&nbsp;Distribution of OpenTelemetry&nbsp;iOS", VersioningSystemId.EdotIos),
		["edot-java"] = new ApplicabilityDefinition("EDOT Java", "Elastic&nbsp;Distribution of OpenTelemetry&nbsp;Java", VersioningSystemId.EdotJava),
		["edot-node"] = new ApplicabilityDefinition("EDOT Node.js", "Elastic&nbsp;Distribution of OpenTelemetry&nbsp;Node.js", VersioningSystemId.EdotNode),
		["edot-php"] = new ApplicabilityDefinition("EDOT PHP", "Elastic&nbsp;Distribution of OpenTelemetry&nbsp;PHP", VersioningSystemId.EdotPhp),
		["edot-python"] = new ApplicabilityDefinition("EDOT Python", "Elastic&nbsp;Distribution of OpenTelemetry&nbsp;Python", VersioningSystemId.EdotPython),

		// APM Agents
		["apm-agent-android"] = new ApplicabilityDefinition("APM Agent Android", "Application&nbsp;Performance&nbsp;Monitoring Agent for Android", VersioningSystemId.ApmAgentAndroid),
		["apm-agent-dotnet"] = new ApplicabilityDefinition("APM Agent .NET", "Application&nbsp;Performance&nbsp;Monitoring Agent for .NET", VersioningSystemId.ApmAgentDotnet),
		["apm-agent-go"] = new ApplicabilityDefinition("APM Agent Go", "Application&nbsp;Performance&nbsp;Monitoring Agent for Go", VersioningSystemId.ApmAgentGo),
		["apm-agent-ios"] = new ApplicabilityDefinition("APM Agent iOS", "Application&nbsp;Performance&nbsp;Monitoring Agent for iOS", VersioningSystemId.ApmAgentIos),
		["apm-agent-java"] = new ApplicabilityDefinition("APM Agent Java", "Application&nbsp;Performance&nbsp;Monitoring Agent for Java", VersioningSystemId.ApmAgentJava),
		["apm-agent-node"] = new ApplicabilityDefinition("APM Agent Node.js", "Application&nbsp;Performance&nbsp;Monitoring Agent for Node.js", VersioningSystemId.ApmAgentNode),
		["apm-agent-php"] = new ApplicabilityDefinition("APM Agent PHP", "Application&nbsp;Performance&nbsp;Monitoring Agent for PHP", VersioningSystemId.ApmAgentPhp),
		["apm-agent-python"] = new ApplicabilityDefinition("APM Agent Python", "Application&nbsp;Performance&nbsp;Monitoring Agent for Python", VersioningSystemId.ApmAgentPython),
		["apm-agent-ruby"] = new ApplicabilityDefinition("APM Agent Ruby", "Application&nbsp;Performance&nbsp;Monitoring Agent for Ruby", VersioningSystemId.ApmAgentRuby),
		["apm-agent-rum"] = new ApplicabilityDefinition("APM Agent RUM", "Application&nbsp;Performance&nbsp;Monitoring Agent for Real&nbsp;User&nbsp;Monitoring", VersioningSystemId.ApmAgentRum),

		// Generic product
		["product"] = new ApplicabilityDefinition("", "", VersioningSystemId.All)
	};

	public static ApplicabilityDefinition GetProductDefinition(string productKey) => Mappings.TryGetValue(productKey, out var definition)
			? definition
			: throw new ArgumentException($"Unknown product key: {productKey}");
}
