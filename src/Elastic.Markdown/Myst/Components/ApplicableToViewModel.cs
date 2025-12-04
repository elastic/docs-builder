// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Markdown.Myst.Components;

public class ApplicableToViewModel
{
	private readonly ApplicabilityRenderer _applicabilityRenderer = new();

	public required bool Inline { get; init; }

	public bool ShowTooltip { get; init; } = true;
	public required ApplicableTo AppliesTo { get; init; }
	public required VersionsConfiguration VersionsConfig { get; init; }

	private static readonly Dictionary<Func<DeploymentApplicability, AppliesCollection?>, ApplicabilityMappings.ApplicabilityDefinition> DeploymentMappings = new()
	{
		[d => d.Ess] = ApplicabilityMappings.Ech,
		[d => d.Eck] = ApplicabilityMappings.Eck,
		[d => d.Ece] = ApplicabilityMappings.Ece,
		[d => d.Self] = ApplicabilityMappings.Self
	};

	private static readonly Dictionary<Func<ServerlessProjectApplicability, AppliesCollection?>, ApplicabilityMappings.ApplicabilityDefinition> ServerlessMappings = new()
	{
		[s => s.Elasticsearch] = ApplicabilityMappings.ServerlessElasticsearch,
		[s => s.Observability] = ApplicabilityMappings.ServerlessObservability,
		[s => s.Security] = ApplicabilityMappings.ServerlessSecurity
	};

	private static readonly Dictionary<Func<ProductApplicability, AppliesCollection?>, ApplicabilityMappings.ApplicabilityDefinition> ProductMappings = new()
	{
		[p => p.Ecctl] = ApplicabilityMappings.Ecctl,
		[p => p.Curator] = ApplicabilityMappings.Curator,
		[p => p.EdotAndroid] = ApplicabilityMappings.EdotAndroid,
		[p => p.EdotCfAws] = ApplicabilityMappings.EdotCfAws,
		[p => p.EdotCfAzure] = ApplicabilityMappings.EdotCfAzure,
		[p => p.EdotCfGcp] = ApplicabilityMappings.EdotCfGcp,
		[p => p.EdotCollector] = ApplicabilityMappings.EdotCollector,
		[p => p.EdotDotnet] = ApplicabilityMappings.EdotDotnet,
		[p => p.EdotIos] = ApplicabilityMappings.EdotIos,
		[p => p.EdotJava] = ApplicabilityMappings.EdotJava,
		[p => p.EdotNode] = ApplicabilityMappings.EdotNode,
		[p => p.EdotPhp] = ApplicabilityMappings.EdotPhp,
		[p => p.EdotPython] = ApplicabilityMappings.EdotPython,
		[p => p.ApmAgentAndroid] = ApplicabilityMappings.ApmAgentAndroid,
		[p => p.ApmAgentDotnet] = ApplicabilityMappings.ApmAgentDotnet,
		[p => p.ApmAgentGo] = ApplicabilityMappings.ApmAgentGo,
		[p => p.ApmAgentIos] = ApplicabilityMappings.ApmAgentIos,
		[p => p.ApmAgentJava] = ApplicabilityMappings.ApmAgentJava,
		[p => p.ApmAgentNode] = ApplicabilityMappings.ApmAgentNode,
		[p => p.ApmAgentPhp] = ApplicabilityMappings.ApmAgentPhp,
		[p => p.ApmAgentPython] = ApplicabilityMappings.ApmAgentPython,
		[p => p.ApmAgentRuby] = ApplicabilityMappings.ApmAgentRuby,
		[p => p.ApmAgentRumJs] = ApplicabilityMappings.ApmAgentRumJs
	};


	public IEnumerable<ApplicabilityItem> GetApplicabilityItems()
	{
		var items = new List<ApplicabilityItem>();

		if (AppliesTo.Serverless is not null)
		{
			items.AddRange(AppliesTo.Serverless.AllProjects is not null
				? ProcessSingleCollection(AppliesTo.Serverless.AllProjects, ApplicabilityMappings.Serverless)
				: ProcessMappedCollections(AppliesTo.Serverless, ServerlessMappings));
		}

		if (AppliesTo.Stack is not null)
			items.AddRange(ProcessSingleCollection(AppliesTo.Stack, ApplicabilityMappings.Stack));

		if (AppliesTo.Deployment is not null)
			items.AddRange(ProcessMappedCollections(AppliesTo.Deployment, DeploymentMappings));

		if (AppliesTo.ProductApplicability is not null)
			items.AddRange(ProcessMappedCollections(AppliesTo.ProductApplicability, ProductMappings));

		if (AppliesTo.Product is not null)
			items.AddRange(ProcessSingleCollection(AppliesTo.Product, ApplicabilityMappings.Product));

		return CombineItemsByKey(items);
	}

	private IEnumerable<ApplicabilityItem> ProcessSingleCollection(AppliesCollection collection, ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition)
	{
		var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDefinition.VersioningSystemId);
		return ProcessApplicabilityCollection(collection, applicabilityDefinition, versioningSystem);
	}

	/// <summary>
	/// Uses mapping dictionary to eliminate repetitive code when processing multiple collections
	/// </summary>
	private IEnumerable<ApplicabilityItem> ProcessMappedCollections<T>(T source, Dictionary<Func<T, AppliesCollection?>, ApplicabilityMappings.ApplicabilityDefinition> mappings)
	{
		var items = new List<ApplicabilityItem>();

		foreach (var (propertySelector, applicabilityDefinition) in mappings)
		{
			var collection = propertySelector(source);
			if (collection is not null)
				items.AddRange(ProcessSingleCollection(collection, applicabilityDefinition));
		}

		return items;
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
				PrimaryApplicability: applicability,
				RenderData: renderData,
				ApplicabilityDefinition: applicabilityDefinition
			);
		});

	/// <summary>
	/// Combines multiple applicability items with the same key into a single item with combined tooltip
	/// </summary>
	private IEnumerable<ApplicabilityItem> CombineItemsByKey(List<ApplicabilityItem> items) => items
			.GroupBy(item => item.Key)
			.Select(group =>
			{
				if (group.Count() == 1)
					return group.First();

				var firstItem = group.First();
				var allApplicabilities = group.Select(g => g.Applicability).ToList();
				var applicabilityDefinition = firstItem.ApplicabilityDefinition;
				var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDefinition.VersioningSystemId);

				var combinedRenderData = _applicabilityRenderer.RenderCombinedApplicability(
					allApplicabilities,
					applicabilityDefinition,
					versioningSystem,
					new AppliesCollection(allApplicabilities.ToArray()));

				// Select the closest version to current as the primary display
				var primaryApplicability = ApplicabilitySelector.GetPrimaryApplicability(allApplicabilities, versioningSystem.Current);

				return new ApplicabilityItem(
					Key: firstItem.Key,
					PrimaryApplicability: primaryApplicability,
					RenderData: combinedRenderData,
					ApplicabilityDefinition: applicabilityDefinition
				);
			});



}
