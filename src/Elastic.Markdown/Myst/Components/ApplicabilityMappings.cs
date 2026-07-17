// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Markdown.Myst.Components;

public static class ApplicabilityMappings
{
	public record ApplicabilityDefinition(string Key, string DisplayName, VersioningSystemId VersioningSystemId);

	// Stack
	public static readonly ApplicabilityDefinition Stack = new("Stack", "Elastic&nbsp;Stack", VersioningSystemId.Stack);

	// Serverless
	public static readonly ApplicabilityDefinition Serverless = new("Serverless", "Elastic&nbsp;Cloud&nbsp;Serverless", VersioningSystemId.Serverless);
	public static readonly ApplicabilityDefinition ServerlessElasticsearch = new("Serverless Elasticsearch", "Serverless&nbsp;Elasticsearch projects", VersioningSystemId.ElasticsearchProject);
	public static readonly ApplicabilityDefinition ServerlessObservability = new("Serverless Observability", "Serverless&nbsp;Observability projects", VersioningSystemId.ObservabilityProject);
	public static readonly ApplicabilityDefinition ServerlessSecurity = new("Serverless Security", "Serverless&nbsp;Security projects", VersioningSystemId.SecurityProject);

	// Deployment
	public static readonly ApplicabilityDefinition Ech = new("ECH", "Elastic&nbsp;Cloud&nbsp;Hosted", VersioningSystemId.Ess);
	public static readonly ApplicabilityDefinition Eck = new("ECK", "Elastic&nbsp;Cloud&nbsp;on&nbsp;Kubernetes", VersioningSystemId.Eck);
	public static readonly ApplicabilityDefinition Ece = new("ECE", "Elastic&nbsp;Cloud&nbsp;Enterprise", VersioningSystemId.Ece);
	public static readonly ApplicabilityDefinition Self = new("Self-managed", "Self-managed Elastic&nbsp;deployments", VersioningSystemId.Self);

	// Product Applicability
	public static readonly ApplicabilityDefinition Ecctl = new("ECCTL", "Elastic&nbsp;Cloud&nbsp;Control", VersioningSystemId.Ecctl);
	public static readonly ApplicabilityDefinition Curator = new("Curator", "Curator", VersioningSystemId.Curator);

	// Elastic OTel Products
	public static readonly ApplicabilityDefinition EdotAndroid = new("Elastic OTel Android", "Elastic OTel Android SDK", VersioningSystemId.EdotAndroid);
	public static readonly ApplicabilityDefinition EdotCfAws = new("Elastic CF AWS", "Elastic Cloud Forwarder for AWS", VersioningSystemId.EdotCfAws);
	public static readonly ApplicabilityDefinition EdotCfAzure = new("Elastic CF Azure", "Elastic Cloud Forwarder for Azure", VersioningSystemId.EdotCfAzure);
	public static readonly ApplicabilityDefinition EdotCfGcp = new("Elastic CF GCP", "Elastic Cloud Forwarder for GCP", VersioningSystemId.EdotCfGcp);
	public static readonly ApplicabilityDefinition EdotCollector = new("Elastic Agent", "Elastic Agent", VersioningSystemId.EdotCollector);
	public static readonly ApplicabilityDefinition EdotDotnet = new("Elastic OTel .NET", "Elastic OTel .NET SDK", VersioningSystemId.EdotDotnet);
	public static readonly ApplicabilityDefinition EdotIos = new("Elastic OTel iOS", "Elastic OTel iOS SDK", VersioningSystemId.EdotIos);
	public static readonly ApplicabilityDefinition EdotJava = new("Elastic OTel Java", "Elastic OTel Java SDK", VersioningSystemId.EdotJava);
	public static readonly ApplicabilityDefinition EdotNode = new("Elastic OTel Node.js", "Elastic OTel Node.js SDK", VersioningSystemId.EdotNode);
	public static readonly ApplicabilityDefinition EdotBrowser = new("Elastic OTel Browser", "Elastic OTel Browser SDK (RUM)", VersioningSystemId.EdotBrowser);
	public static readonly ApplicabilityDefinition EdotPhp = new("Elastic OTel PHP", "Elastic OTel PHP SDK", VersioningSystemId.EdotPhp);
	public static readonly ApplicabilityDefinition EdotPython = new("Elastic OTel Python", "Elastic OTel Python SDK", VersioningSystemId.EdotPython);

	// APM Agents
	public static readonly ApplicabilityDefinition ApmAgentAndroid = new("APM Agent Android", "Application&nbsp;Performance&nbsp;Monitoring Agent for Android", VersioningSystemId.ApmAgentAndroid);
	public static readonly ApplicabilityDefinition ApmAgentDotnet = new("APM Agent .NET", "Application&nbsp;Performance&nbsp;Monitoring Agent for .NET", VersioningSystemId.ApmAgentDotnet);
	public static readonly ApplicabilityDefinition ApmAgentGo = new("APM Agent Go", "Application&nbsp;Performance&nbsp;Monitoring Agent for Go", VersioningSystemId.ApmAgentGo);
	public static readonly ApplicabilityDefinition ApmAgentIos = new("APM Agent iOS", "Application&nbsp;Performance&nbsp;Monitoring Agent for iOS", VersioningSystemId.ApmAgentIos);
	public static readonly ApplicabilityDefinition ApmAgentJava = new("APM Agent Java", "Application&nbsp;Performance&nbsp;Monitoring Agent for Java", VersioningSystemId.ApmAgentJava);
	public static readonly ApplicabilityDefinition ApmAgentNode = new("APM Agent Node.js", "Application&nbsp;Performance&nbsp;Monitoring Agent for Node.js", VersioningSystemId.ApmAgentNode);
	public static readonly ApplicabilityDefinition ApmAgentPhp = new("APM Agent PHP", "Application&nbsp;Performance&nbsp;Monitoring Agent for PHP", VersioningSystemId.ApmAgentPhp);
	public static readonly ApplicabilityDefinition ApmAgentPython = new("APM Agent Python", "Application&nbsp;Performance&nbsp;Monitoring Agent for Python", VersioningSystemId.ApmAgentPython);
	public static readonly ApplicabilityDefinition ApmAgentRuby = new("APM Agent Ruby", "Application&nbsp;Performance&nbsp;Monitoring Agent for Ruby", VersioningSystemId.ApmAgentRuby);
	public static readonly ApplicabilityDefinition ApmAgentRumJs = new("APM Agent RUM", "Application&nbsp;Performance&nbsp;Monitoring Agent for Real&nbsp;User&nbsp;Monitoring", VersioningSystemId.ApmAgentRumJs);

	// Generic product
	public static readonly ApplicabilityDefinition Product = new("", "", VersioningSystemId.All);

}
