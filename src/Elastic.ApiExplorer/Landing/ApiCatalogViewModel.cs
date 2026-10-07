// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Site.Icons;

namespace Elastic.ApiExplorer.Landing;

/// <summary>A deployment an API is available on. <see cref="Label"/> is the short tag text, <see cref="Name"/> the full name.</summary>
public sealed record ApiCatalogDeployment(string Label, string Name);

public sealed record ApiCatalogItem(
	string Key,
	string Title,
	string Url,
	string IconSvg,
	string? Summary,
	IReadOnlyList<ApiCatalogDeployment> Deployments
);

/// <summary>Input for the card partial. <see cref="Featured"/> selects the larger card used for the top pair.</summary>
public sealed record ApiCatalogCard(ApiCatalogItem Item, bool Featured);

public class ApiCatalogViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	// Most traffic lands on these two, so they get the large cards at the top of the page.
	private static readonly string[] FeaturedKeys = ["elasticsearch", "kibana"];

	// Listed first, in this order, so the most used APIs and their variants sit together.
	// Everything else sorts by title after them.
	private static readonly string[] PriorityKeys = [.. FeaturedKeys, "elasticsearch-serverless", "kibana-serverless", "logstash", "cloud"];

	private static readonly (string Category, string Label)[] DeploymentLabels =
	[
		(ApplicabilityKeys.Self, "Self-managed"),
		(ApplicabilityKeys.Ece, "ECE"),
		(ApplicabilityKeys.Ess, "ECH"),
		(ApplicabilityKeys.Serverless, "Serverless")
	];

	private const string FallbackIconSvg =
		"""<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m8 8-4 4 4 4M16 8l4 4-4 4M13.5 5 10.5 19"/></svg>""";

	public required IReadOnlyList<ApiCatalogItem> Featured { get; init; }

	public required IReadOnlyList<ApiCatalogItem> Others { get; init; }

	protected override string? LayoutPageDescription => ApiCatalog.PageDescription;

	public static ApiCatalogViewModel FromEntries(ApiRenderContext context, IReadOnlyList<ApiCatalogEntry> entries)
	{
		var (featured, others) = Split(entries);
		return new(context) { Featured = [.. featured.Select(ToItem)], Others = [.. others.Select(ToItem)] };
	}

	/// <summary>
	/// The featured pair is only split out when both are present. A docset without one of them
	/// gets a plain grid instead of a half-empty featured row.
	/// </summary>
	internal static (ApiCatalogEntry[] Featured, ApiCatalogEntry[] Others) Split(IEnumerable<ApiCatalogEntry> entries)
	{
		var ordered = Order(entries);
		var featured = ordered.Where(e => FeaturedKeys.Contains(e.Key)).ToArray();
		return featured.Length == FeaturedKeys.Length ? (featured, [.. ordered.Except(featured)]) : ([], ordered);
	}

	internal static ApiCatalogEntry[] Order(IEnumerable<ApiCatalogEntry> entries) =>
		[
			.. entries
				.OrderBy(e => PriorityRank(e.Key))
				.ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
				.ThenBy(e => e.Key, StringComparer.Ordinal)
		];

	internal static IReadOnlyList<ApiCatalogDeployment> DeploymentsOf(ApiCatalogEntry entry) =>
		[
			.. DeploymentLabels.Where(d => entry.CatalogCategories.Contains(d.Category)).Select(
				d => new ApiCatalogDeployment(d.Label, ApiCatalogCategory.DisplayName(d.Category))
			)
		];

	private static int PriorityRank(string key)
	{
		var index = Array.IndexOf(PriorityKeys, key);
		return index < 0 ? PriorityKeys.Length : index;
	}

	private static ApiCatalogItem ToItem(ApiCatalogEntry entry) =>
		new(
			entry.Key,
			entry.Title,
			entry.Url,
			ProductIcons.Get(entry.ProductId ?? entry.Key) ?? FallbackIconSvg,
			ApiSeoDescription.Excerpt(entry.Description),
			DeploymentsOf(entry)
		);
}
