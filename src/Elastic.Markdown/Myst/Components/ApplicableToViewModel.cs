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
			var applicabilityDef = ApplicabilityMappings.Stack;
			var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(AppliesTo.Stack, applicabilityDef, versioningSystem));
		}

		// Process Serverless
		if (AppliesTo.Serverless is not null)
		{
			if (AppliesTo.Serverless.AllProjects is not null)
			{
				var applicabilityDef = ApplicabilityMappings.Serverless;
				var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDef.VersioningSystemId);
				items.AddRange(ProcessApplicabilityCollection(AppliesTo.Serverless.AllProjects, applicabilityDef, versioningSystem));
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
			var applicabilityDef = ApplicabilityMappings.Product;
			var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(AppliesTo.Product, applicabilityDef, versioningSystem));
		}

		return items;
	}

	private IEnumerable<ApplicabilityItem> ProcessServerlessProjects(ServerlessProjectApplicability serverless)
	{
		var items = new List<ApplicabilityItem>();

		if (serverless.Elasticsearch is not null)
		{
			var applicabilityDef = ApplicabilityMappings.ServerlessElasticsearch;
			var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(serverless.Elasticsearch, applicabilityDef, versioningSystem));
		}

		if (serverless.Observability is not null)
		{
			var applicabilityDef = ApplicabilityMappings.ServerlessObservability;
			var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(serverless.Observability, applicabilityDef, versioningSystem));
		}

		if (serverless.Security is not null)
		{
			var applicabilityDef = ApplicabilityMappings.ServerlessSecurity;
			var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(serverless.Security, applicabilityDef, versioningSystem));
		}

		return items;
	}

	private IEnumerable<ApplicabilityItem> ProcessDeploymentTypes(DeploymentApplicability deployment)
	{
		var items = new List<ApplicabilityItem>();

		if (deployment.Ess is not null)
		{
			var applicabilityDef = ApplicabilityMappings.Ech;
			var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(deployment.Ess, applicabilityDef, versioningSystem));
		}

		if (deployment.Eck is not null)
		{
			var applicabilityDef = ApplicabilityMappings.Eck;
			var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(deployment.Eck, applicabilityDef, versioningSystem));
		}

		if (deployment.Ece is not null)
		{
			var applicabilityDef = ApplicabilityMappings.Ece;
			var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(deployment.Ece, applicabilityDef, versioningSystem));
		}

		if (deployment.Self is not null)
		{
			var applicabilityDef = ApplicabilityMappings.Self;
			var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDef.VersioningSystemId);
			items.AddRange(ProcessApplicabilityCollection(deployment.Self, applicabilityDef, versioningSystem));
		}

		return items;
	}

	private IEnumerable<ApplicabilityItem> ProcessProductApplicability(ProductApplicability productApplicability)
	{
		var items = new List<ApplicabilityItem>();

		// Process each product applicability property explicitly (AOT-compatible) using strongly-typed definitions
		ProcessProductIfNotNull(productApplicability.Ecctl, ApplicabilityMappings.Ecctl, items);
		ProcessProductIfNotNull(productApplicability.Curator, ApplicabilityMappings.Curator, items);
		ProcessProductIfNotNull(productApplicability.EdotAndroid, ApplicabilityMappings.EdotAndroid, items);
		ProcessProductIfNotNull(productApplicability.EdotCfAws, ApplicabilityMappings.EdotCfAws, items);
		ProcessProductIfNotNull(productApplicability.EdotCollector, ApplicabilityMappings.EdotCollector, items);
		ProcessProductIfNotNull(productApplicability.EdotDotnet, ApplicabilityMappings.EdotDotnet, items);
		ProcessProductIfNotNull(productApplicability.EdotIos, ApplicabilityMappings.EdotIos, items);
		ProcessProductIfNotNull(productApplicability.EdotJava, ApplicabilityMappings.EdotJava, items);
		ProcessProductIfNotNull(productApplicability.EdotNode, ApplicabilityMappings.EdotNode, items);
		ProcessProductIfNotNull(productApplicability.EdotPhp, ApplicabilityMappings.EdotPhp, items);
		ProcessProductIfNotNull(productApplicability.EdotPython, ApplicabilityMappings.EdotPython, items);
		ProcessProductIfNotNull(productApplicability.ApmAgentAndroid, ApplicabilityMappings.ApmAgentAndroid, items);
		ProcessProductIfNotNull(productApplicability.ApmAgentDotnet, ApplicabilityMappings.ApmAgentDotnet, items);
		ProcessProductIfNotNull(productApplicability.ApmAgentGo, ApplicabilityMappings.ApmAgentGo, items);
		ProcessProductIfNotNull(productApplicability.ApmAgentIos, ApplicabilityMappings.ApmAgentIos, items);
		ProcessProductIfNotNull(productApplicability.ApmAgentJava, ApplicabilityMappings.ApmAgentJava, items);
		ProcessProductIfNotNull(productApplicability.ApmAgentNode, ApplicabilityMappings.ApmAgentNode, items);
		ProcessProductIfNotNull(productApplicability.ApmAgentPhp, ApplicabilityMappings.ApmAgentPhp, items);
		ProcessProductIfNotNull(productApplicability.ApmAgentPython, ApplicabilityMappings.ApmAgentPython, items);
		ProcessProductIfNotNull(productApplicability.ApmAgentRuby, ApplicabilityMappings.ApmAgentRuby, items);
		ProcessProductIfNotNull(productApplicability.ApmAgentRum, ApplicabilityMappings.ApmAgentRum, items);

		return items;
	}

	private void ProcessProductIfNotNull(AppliesCollection? collection, ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition, List<ApplicabilityItem> items)
	{
		if (collection is null)
			return;

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
