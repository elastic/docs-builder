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
		var rawItems = new List<RawApplicabilityItem>();

		if (AppliesTo.Serverless is not null)
		{
			rawItems.AddRange(AppliesTo.Serverless.AllProjects is not null
				? CollectFromCollection(AppliesTo.Serverless.AllProjects, ApplicabilityMappings.Serverless)
				: CollectFromMappings(AppliesTo.Serverless, ServerlessMappings));
		}

		if (AppliesTo.Stack is not null)
			rawItems.AddRange(CollectFromCollection(AppliesTo.Stack, ApplicabilityMappings.Stack));

		if (AppliesTo.Deployment is not null)
			rawItems.AddRange(CollectFromMappings(AppliesTo.Deployment, DeploymentMappings));

		if (AppliesTo.ProductApplicability is not null)
			rawItems.AddRange(CollectFromMappings(AppliesTo.ProductApplicability, ProductMappings));

		if (AppliesTo.Product is not null)
			rawItems.AddRange(CollectFromCollection(AppliesTo.Product, ApplicabilityMappings.Product));

		return RenderGroupedItems(rawItems);
	}

	/// <summary>
	/// Collects raw applicability items from a single collection.
	/// </summary>
	private static IEnumerable<RawApplicabilityItem> CollectFromCollection(
		AppliesCollection collection,
		ApplicabilityMappings.ApplicabilityDefinition applicabilityDefinition) =>
		collection.Select(applicability => new RawApplicabilityItem(
			Key: applicabilityDefinition.Key,
			Applicability: applicability,
			ApplicabilityDefinition: applicabilityDefinition
		));

	/// <summary>
	/// Collects raw applicability items from mapped collections.
	/// </summary>
	private static IEnumerable<RawApplicabilityItem> CollectFromMappings<T>(
		T source,
		Dictionary<Func<T, AppliesCollection?>, ApplicabilityMappings.ApplicabilityDefinition> mappings)
	{
		var items = new List<RawApplicabilityItem>();

		foreach (var (propertySelector, applicabilityDefinition) in mappings)
		{
			var collection = propertySelector(source);
			if (collection is not null)
				items.AddRange(CollectFromCollection(collection, applicabilityDefinition));
		}

		return items;
	}

	/// <summary>
	/// Groups raw items by key and renders each group using the unified renderer.
	/// </summary>
	private IEnumerable<ApplicabilityItem> RenderGroupedItems(List<RawApplicabilityItem> rawItems) =>
		rawItems
			.GroupBy(item => item.Key)
			.Select(group =>
			{
				var items = group.ToList();
				var applicabilityDefinition = items.First().ApplicabilityDefinition;
				var versioningSystem = VersionsConfig.GetVersioningSystem(applicabilityDefinition.VersioningSystemId);
				var allApplicabilities = items.Select(i => i.Applicability).ToList();

				var renderData = _applicabilityRenderer.RenderApplicability(
					allApplicabilities,
					applicabilityDefinition,
					versioningSystem);

				// Select the closest version to current as the primary display
				var primaryApplicability = ApplicabilitySelector.GetPrimaryApplicability(allApplicabilities, versioningSystem.Current);

				return new ApplicabilityItem(
					Key: items.First().Key,
					PrimaryApplicability: primaryApplicability,
					RenderData: renderData,
					ApplicabilityDefinition: applicabilityDefinition
				);
			});

	/// <summary>
	/// Intermediate representation before rendering.
	/// </summary>
	private sealed record RawApplicabilityItem(
		string Key,
		Applicability Applicability,
		ApplicabilityMappings.ApplicabilityDefinition ApplicabilityDefinition
	);
}
