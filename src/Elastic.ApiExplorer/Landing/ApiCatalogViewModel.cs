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
	IReadOnlyList<ApiCatalogDeployment> Deployments,
	string? ParentKey = null
);

/// <summary>
/// One labelled band of the page. <see cref="Title"/> is null for a lone group, which needs no label. A
/// <see cref="Grouped"/> group is one of the defined ones, as opposed to the APIs that no group covers.
/// </summary>
public sealed record ApiCatalogGroup(
	string? Title,
	bool Grouped,
	IReadOnlyList<ApiCatalogItem> Featured,
	IReadOnlyList<ApiCatalogItem> Others
)
{
	/// <summary>True when a tag-on in this group is attached under <paramref name="featured"/>.</summary>
	public bool HasVariant(ApiCatalogItem featured) => Others.Any(o => o.ParentKey == featured.Key);

	/// <summary>The 1-based grid column of the featured card that <paramref name="variant"/> is attached under, if it has one here.</summary>
	public int? ColumnOf(ApiCatalogItem variant)
	{
		for (var i = 0; i < Featured.Count; i++)
		{
			if (Featured[i].Key == variant.ParentKey)
				return i + 1;
		}
		return null;
	}
}

/// <summary>The entries of one group, in display order: the featured ones first, then the rest.</summary>
internal sealed record ApiCatalogEntryGroup(string? Title, bool Grouped, ApiCatalogEntry[] Featured, ApiCatalogEntry[] Others)
{
	public ApiCatalogEntry[] Entries => [.. Featured, .. Others];
}

public enum ApiCatalogCardKind
{
	/// <summary>The full-size card, with the deployment pills.</summary>
	Standard,

	/// <summary>The larger card for the core APIs.</summary>
	Featured,

	/// <summary>The small card for everything else in a group.</summary>
	Compact
}

/// <summary>
/// Input for the card partial. <see cref="HasVariant"/> marks a featured card with a tag-on attached under it.
/// <see cref="Column"/> is the grid column of the card a tag-on is attached under.
/// </summary>
public sealed record ApiCatalogCard(
	ApiCatalogItem Item,
	ApiCatalogCardKind Kind = ApiCatalogCardKind.Standard,
	bool HasVariant = false,
	int? Column = null
);

public class ApiCatalogViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	// The core stack APIs get the large cards at the top of their group.
	private static readonly string[] FeaturedKeys = ["elasticsearch", "kibana", "logstash"];

	// A variant is shown as a tag-on, attached under the card of the API it is a variant of.
	private static readonly Dictionary<string, string> VariantParents = new(StringComparer.Ordinal)
	{
		["elasticsearch-serverless"] = "elasticsearch",
		["kibana-serverless"] = "kibana"
	};

	// Each API belongs to exactly one group. Keys list the display order inside it. The titles are
	// provisional while we collect naming feedback. An API in neither group follows them, ordered by title.
	private static readonly (string Title, string[] Keys)[] GroupDefinitions =
	[
		("Elastic Stack", [.. FeaturedKeys, "elasticsearch-serverless", "kibana-serverless"]),
		("Orchestration", ["cloud", "cloud-billing", "cloud-connect", "cloud-enterprise", "cloud-serverless"])
	];

	private static readonly (string Category, string Label)[] DeploymentLabels =
	[
		(ApplicabilityKeys.Self, "Self-managed"),
		(ApplicabilityKeys.Ece, "ECE"),
		(ApplicabilityKeys.Ess, "ECH"),
		(ApplicabilityKeys.Serverless, "Serverless")
	];

	private const string FallbackIconSvg =
		"""<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m8 8-4 4 4 4M16 8l4 4-4 4M13.5 5 10.5 19"/></svg>""";

	public required IReadOnlyList<ApiCatalogGroup> Groups { get; init; }

	protected override string? LayoutPageDescription => ApiCatalog.PageDescription;

	public static ApiCatalogViewModel FromEntries(ApiRenderContext context, IReadOnlyList<ApiCatalogEntry> entries) =>
		new(context)
		{
			Groups =
			[
				.. Group(entries).Select(
					g => new ApiCatalogGroup(g.Title, g.Grouped, [.. g.Featured.Select(ToItem)], [.. g.Others.Select(ToItem)])
				)
			]
		};

	/// <summary>
	/// Sorts every entry into its group. Empty groups are dropped, and a single remaining group loses its
	/// title, so a docset with only a few APIs gets a plain grid instead of a lone heading.
	/// </summary>
	internal static IReadOnlyList<ApiCatalogEntryGroup> Group(IEnumerable<ApiCatalogEntry> entries)
	{
		var remaining = entries.ToList();
		var groups = new List<ApiCatalogEntryGroup>();
		foreach (var (title, keys) in GroupDefinitions)
		{
			var members = remaining.Where(e => keys.Contains(e.Key)).OrderBy(e => Array.IndexOf(keys, e.Key)).ToArray();
			_ = remaining.RemoveAll(e => keys.Contains(e.Key));
			if (members.Length > 0)
				groups.Add(WithFeatured(title, members));
		}
		if (remaining.Count > 0)
		{
			ApiCatalogEntry[] rest =
			[
				.. remaining.OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Key, StringComparer.Ordinal)
			];
			groups.Add(new(null, Grouped: false, [], rest));
		}

		return groups.Count == 1 ? [groups[0] with { Title = null }] : groups;
	}

	/// <summary>
	/// The featured cards are only split out when at least two are present. A group with fewer
	/// gets a plain grid instead of a lone large card.
	/// </summary>
	private static ApiCatalogEntryGroup WithFeatured(string title, ApiCatalogEntry[] members)
	{
		var featured = members.Where(e => FeaturedKeys.Contains(e.Key)).OrderBy(e => Array.IndexOf(FeaturedKeys, e.Key)).ToArray();
		return featured.Length >= 2
			? new(title, Grouped: true, featured, [.. members.Except(featured)])
			: new(title, Grouped: true, [], members);
	}

	internal static IReadOnlyList<ApiCatalogDeployment> DeploymentsOf(ApiCatalogEntry entry) =>
		[
			.. DeploymentLabels.Where(d => entry.CatalogCategories.Contains(d.Category)).Select(
				d => new ApiCatalogDeployment(d.Label, ApiCatalogCategory.DisplayName(d.Category))
			)
		];

	private static ApiCatalogItem ToItem(ApiCatalogEntry entry) =>
		new(
			entry.Key,
			entry.Title,
			entry.Url,
			ProductIcons.Get(entry.ProductId ?? entry.Key) ?? FallbackIconSvg,
			ApiSeoDescription.Excerpt(entry.Description),
			DeploymentsOf(entry),
			VariantParents.GetValueOrDefault(entry.Key)
		);
}
