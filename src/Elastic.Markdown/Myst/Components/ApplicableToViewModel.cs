// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Markdown.Myst.Components;

public class ApplicableToViewModel
{
	private readonly ApplicabilityRenderer _applicabilityRenderer = new();

	public required bool Inline { get; init; }
	public required ApplicableTo AppliesTo { get; init; }
	public required VersionsConfiguration VersionsConfig { get; init; }

	public IEnumerable<ApplicabilityItem> GetApplicabilityItems()
	{
		var items = new List<ApplicabilityItem>();

		// Process Stack
		if (AppliesTo.Stack is not null)
		{
			var productDef = ApplicabilityMappings.GetProductDefinition("stack");
			var versioningSystem = VersionsConfig.GetVersioningSystem(productDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(AppliesTo.Stack, productDef, versioningSystem));
		}

		// Process Serverless
		if (AppliesTo.Serverless is not null)
		{
			if (AppliesTo.Serverless.AllProjects is not null)
			{
				var productDef = ApplicabilityMappings.GetProductDefinition("serverless");
				var versioningSystem = VersionsConfig.GetVersioningSystem(productDef.VersioningSystemId);
				items.AddRange(ProcessApplicabilityCollection(AppliesTo.Serverless.AllProjects, productDef, versioningSystem));
			}
			else
				items.AddRange(ProcessServerlessProjects(AppliesTo.Serverless));
		}

		// Process Deployment
		if (AppliesTo.Deployment is not null)
			items.AddRange(ProcessDeploymentTypes(AppliesTo.Deployment));

		// Process Product Applicability
		if (AppliesTo.ProductApplicability is not null)
			items.AddRange(ProcessProductApplicability(AppliesTo.ProductApplicability));

		// Process Generic Product
		if (AppliesTo.Product is not null)
		{
			var productDef = ApplicabilityMappings.GetProductDefinition("product");
			var versioningSystem = VersionsConfig.GetVersioningSystem(productDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(AppliesTo.Product, productDef, versioningSystem));
		}

		return items;
	}

	private IEnumerable<ApplicabilityItem> ProcessServerlessProjects(ServerlessProjectApplicability serverless)
	{
		var items = new List<ApplicabilityItem>();

		if (serverless.Elasticsearch is not null)
		{
			var productDef = ApplicabilityMappings.GetProductDefinition("serverless-elasticsearch");
			var versioningSystem = VersionsConfig.GetVersioningSystem(productDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(serverless.Elasticsearch, productDef, versioningSystem));
		}

		if (serverless.Observability is not null)
		{
			var productDef = ApplicabilityMappings.GetProductDefinition("serverless-observability");
			var versioningSystem = VersionsConfig.GetVersioningSystem(productDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(serverless.Observability, productDef, versioningSystem));
		}

		if (serverless.Security is not null)
		{
			var productDef = ApplicabilityMappings.GetProductDefinition("serverless-security");
			var versioningSystem = VersionsConfig.GetVersioningSystem(productDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(serverless.Security, productDef, versioningSystem));
		}

		return items;
	}

	private IEnumerable<ApplicabilityItem> ProcessDeploymentTypes(DeploymentApplicability deployment)
	{
		var items = new List<ApplicabilityItem>();

		if (deployment.Ess is not null)
		{
			var productDef = ApplicabilityMappings.GetProductDefinition("ech");
			var versioningSystem = VersionsConfig.GetVersioningSystem(productDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(deployment.Ess, productDef, versioningSystem));
		}

		if (deployment.Eck is not null)
		{
			var productDef = ApplicabilityMappings.GetProductDefinition("eck");
			var versioningSystem = VersionsConfig.GetVersioningSystem(productDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(deployment.Eck, productDef, versioningSystem));
		}

		if (deployment.Ece is not null)
		{
			var productDef = ApplicabilityMappings.GetProductDefinition("ece");
			var versioningSystem = VersionsConfig.GetVersioningSystem(productDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(deployment.Ece, productDef, versioningSystem));
		}

		if (deployment.Self is not null)
		{
			var productDef = ApplicabilityMappings.GetProductDefinition("self");
			var versioningSystem = VersionsConfig.GetVersioningSystem(productDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(deployment.Self, productDef, versioningSystem));
		}

		return items;
	}

	private IEnumerable<ApplicabilityItem> ProcessProductApplicability(ProductApplicability productApplicability)
	{
		var items = new List<ApplicabilityItem>();

		// Process each product applicability property explicitly (AOT-compatible)
		ProcessProductIfNotNull(productApplicability.Ecctl, "ecctl", items);
		ProcessProductIfNotNull(productApplicability.Curator, "curator", items);
		ProcessProductIfNotNull(productApplicability.EdotAndroid, "edot-android", items);
		ProcessProductIfNotNull(productApplicability.EdotCfAws, "edot-cf-aws", items);
		ProcessProductIfNotNull(productApplicability.EdotCollector, "edot-collector", items);
		ProcessProductIfNotNull(productApplicability.EdotDotnet, "edot-dotnet", items);
		ProcessProductIfNotNull(productApplicability.EdotIos, "edot-ios", items);
		ProcessProductIfNotNull(productApplicability.EdotJava, "edot-java", items);
		ProcessProductIfNotNull(productApplicability.EdotNode, "edot-node", items);
		ProcessProductIfNotNull(productApplicability.EdotPhp, "edot-php", items);
		ProcessProductIfNotNull(productApplicability.EdotPython, "edot-python", items);
		ProcessProductIfNotNull(productApplicability.ApmAgentAndroid, "apm-agent-android", items);
		ProcessProductIfNotNull(productApplicability.ApmAgentDotnet, "apm-agent-dotnet", items);
		ProcessProductIfNotNull(productApplicability.ApmAgentGo, "apm-agent-go", items);
		ProcessProductIfNotNull(productApplicability.ApmAgentIos, "apm-agent-ios", items);
		ProcessProductIfNotNull(productApplicability.ApmAgentJava, "apm-agent-java", items);
		ProcessProductIfNotNull(productApplicability.ApmAgentNode, "apm-agent-node", items);
		ProcessProductIfNotNull(productApplicability.ApmAgentPhp, "apm-agent-php", items);
		ProcessProductIfNotNull(productApplicability.ApmAgentPython, "apm-agent-python", items);
		ProcessProductIfNotNull(productApplicability.ApmAgentRuby, "apm-agent-ruby", items);
		ProcessProductIfNotNull(productApplicability.ApmAgentRum, "apm-agent-rum", items);

		return items;
	}

	private void ProcessProductIfNotNull(AppliesCollection? collection, string productKey, List<ApplicabilityItem> items)
	{
		if (collection is null)
			return;

		var applicabilityDefinition = ApplicabilityMappings.GetProductDefinition(productKey);
		var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDefinition.VersioningSystemId);
		items.AddRange(ProcessApplicabilityCollection(collection, applicabilityDefinition, versioningSystem));
	}

	private IEnumerable<ApplicabilityItem> ProcessApplicabilityCollection(
		AppliesCollection applications,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition,
		VersioningSystem versioningSystem) =>
		applications.Select(applicability =>
		{
			var renderData = _applicabilityRenderer.RenderApplicability(
				applicability,
				applicabilityDefinition,
				versioningSystem,
				applications);

			return new ApplicabilityItem(
				Key: applicabilityDefinition.Key,
				Applicability: applicability,
				RenderData: renderData
			);
		});

}
