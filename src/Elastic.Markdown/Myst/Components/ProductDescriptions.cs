// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Markdown.Myst.Components;

/// <summary>
/// Contains static product descriptions for use in applicability popovers.
/// </summary>
public static class ProductDescriptions
{
	/// <summary>
	/// Product information record containing description, additional info, and version note flag.
	/// </summary>
	/// <param name="Description">The product description shown at the top of the popover (required).</param>
	/// <param name="AdditionalAvailabilityInfo">Additional availability information shown near the bottom of the popover (optional).</param>
	/// <param name="IncludeVersionNote">Whether to include the version note at the bottom of the popover.</param>
	public record ProductInfo(
		string Description,
		string? AdditionalAvailabilityInfo,
		bool IncludeVersionNote
	);

	/// <summary>
	/// The version note text shown at the bottom of versioned product popovers.
	/// </summary>
	public const string VersionNote =
		"This documentation corresponds to the latest patch available for each minor version. If you're not using the latest patch, check the release notes for changes.";

	public static ProductInfo? GetProductInfo(VersioningSystemId versioningSystemId) =>
		Descriptions.GetValueOrDefault(versioningSystemId);

	private static readonly Dictionary<VersioningSystemId, ProductInfo> Descriptions = new()
	{
		// Stack
		[VersioningSystemId.Stack] = new ProductInfo(
			Description: "The <strong>Elastic Stack</strong> includes Elastic's core products such as Elasticsearch, Kibana, Logstash, and Beats.",
			AdditionalAvailabilityInfo: "Unless stated otherwise on the page, this functionality is available when your Elastic Stack is deployed on Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes, and self-managed environments.",
			IncludeVersionNote: true
		),

		// Serverless
		[VersioningSystemId.Serverless] = new ProductInfo(
			Description: "<strong>Elastic Cloud Serverless</strong> projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.",
			AdditionalAvailabilityInfo: "Serverless interfaces and procedures might differ from classic Elastic Stack deployments.",
			IncludeVersionNote: false
		),

		// Serverless Project Types
		[VersioningSystemId.ElasticsearchProject] = new ProductInfo(
			Description: "<strong>Elastic Cloud Serverless</strong> projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: false
		),
		[VersioningSystemId.ObservabilityProject] = new ProductInfo(
			Description: "<strong>Elastic Cloud Serverless</strong> projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: false
		),
		[VersioningSystemId.SecurityProject] = new ProductInfo(
			Description: "<strong>Elastic Cloud Serverless</strong> projects are autoscaled environments, fully managed by Elastic and available on Elastic Cloud.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: false
		),

		// Deployment Types
		[VersioningSystemId.Ess] = new ProductInfo(
			Description: "<strong>Elastic Cloud Hosted</strong> lets you manage and configure one or more deployments of the versioned Elastic Stack, hosted on Elastic Cloud.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: false
		),
		[VersioningSystemId.Ece] = new ProductInfo(
			Description: "<strong>Elastic Cloud Enterprise</strong> is a self-managed orchestration platform for deploying and managing the Elastic Stack at scale.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.Eck] = new ProductInfo(
			Description: "<strong>Elastic Cloud on Kubernetes</strong> extends Kubernetes orchestration capabilities to allow you to deploy and manage components of the Elastic Stack.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.Self] = new ProductInfo(
			Description: "<strong>Self-managed</strong> deployments are Elastic Stack deployments managed without the assistance of an orchestrator.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),

		// Products
		[VersioningSystemId.Ecctl] = new ProductInfo(
			Description: "<strong>ECCTL</strong> is the command line interface for the Elastic Cloud and Elastic Cloud Enterprise APIs.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.Curator] = new ProductInfo(
			Description: "<strong>Curator</strong> is a tool that helps you to manage your Elasticsearch indices and snapshots to save space and improve performance.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),

		// APM Agents
		[VersioningSystemId.ApmAgentDotnet] = new ProductInfo(
			Description: "The <strong>Elastic APM .NET agent</strong> enables you to trace the execution of operations in your .NET applications, sending performance metrics and errors to the Elastic APM server.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.ApmAgentGo] = new ProductInfo(
			Description: "The <strong>Elastic APM Go agent</strong> enables you to trace the execution of operations in your Go applications, sending performance metrics and errors to the Elastic APM Server.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.ApmAgentJava] = new ProductInfo(
			Description: "The <strong>Elastic APM Java agent</strong> enables you to trace the execution of operations in your Java applications, sending performance metrics and errors to the Elastic APM Server.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.ApmAgentNode] = new ProductInfo(
			Description: "The <strong>Elastic APM Node.js agent</strong> enables you to trace the execution of operations in your Node.js applications, sending performance metrics and errors to the Elastic APM Server.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.ApmAgentPhp] = new ProductInfo(
			Description: "The <strong>Elastic APM PHP agent</strong> enables you to trace the execution of operations in your PHP applications, sending performance metrics and errors to the Elastic APM Server.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.ApmAgentPython] = new ProductInfo(
			Description: "The <strong>Elastic APM Python agent</strong> enables you to trace the execution of operations in your Python applications, sending performance metrics and errors to the Elastic APM Server.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.ApmAgentRuby] = new ProductInfo(
			Description: "The <strong>Elastic APM Ruby agent</strong> enables you to trace the execution of operations in your Ruby applications, sending performance metrics and errors to the Elastic APM Server.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.ApmAgentRumJs] = new ProductInfo(
			Description: "The <strong>Elastic APM RUM JavaScript agent</strong> enables you to trace the execution of operations in your web applications, sending performance metrics and errors to the Elastic APM Server.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),

		// EDOT Products
		[VersioningSystemId.EdotCollector] = new ProductInfo(
			Description: "The <strong>Elastic Distribution of OpenTelemetry (EDOT) Collector</strong> retrieves traces, metrics, and logs from your infrastructure and applications, and forwards them to Elastic Observability.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.EdotIos] = new ProductInfo(
			Description: "The <strong>Elastic Distribution of OpenTelemetry (EDOT) iOS SDK</strong> collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.EdotAndroid] = new ProductInfo(
			Description: "The <strong>Elastic Distribution of OpenTelemetry (EDOT) Android SDK</strong> collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.EdotDotnet] = new ProductInfo(
			Description: "The <strong>Elastic Distribution of OpenTelemetry (EDOT) .NET SDK</strong> collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.EdotJava] = new ProductInfo(
			Description: "The <strong>Elastic Distribution of OpenTelemetry (EDOT) Java SDK</strong> collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.EdotNode] = new ProductInfo(
			Description: "The <strong>Elastic Distribution of OpenTelemetry (EDOT) Node.js SDK</strong> collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.EdotPhp] = new ProductInfo(
			Description: "The <strong>Elastic Distribution of OpenTelemetry (EDOT) PHP SDK</strong> collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.EdotPython] = new ProductInfo(
			Description: "The <strong>Elastic Distribution of OpenTelemetry (EDOT) Python SDK</strong> collects performance metrics, traces, and logs in OpenTelemetry format, and sends them to Elastic Observability.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
		[VersioningSystemId.EdotCfAws] = new ProductInfo(
			Description: "The <strong>Elastic Distribution of OpenTelemetry (EDOT) Cloud Forwarder</strong> allows you to collect and send your telemetry data to Elastic Observability from AWS, GCP, and Azure.",
			AdditionalAvailabilityInfo: null,
			IncludeVersionNote: true
		),
	};
}

