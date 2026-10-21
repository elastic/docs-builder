// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using Nullean.Entities;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>A changelog entry with the configuration (and product catalog) whose type, subtype, lifecycle, area and product lists it must stay within.</summary>
public sealed record ChangelogEntryInConfiguration(ChangelogEntry Entry, ChangelogConfigurationInCatalog Configuration);

/// <summary>Every rule that holds a <see cref="ChangelogEntry"/> to the lists its configuration declares, in the order they are reported.</summary>
[Constraints]
public static partial class ChangelogEntryInConfigurationConstraints
{
	public static Constraint<ChangelogEntryInConfiguration> EntryProductsKnown { get; } = Constraints<ChangelogEntryInConfiguration>.Each(
		Products,
		(m, p) => string.IsNullOrWhiteSpace(p.ProductId) || m.Configuration.IsKnownProduct(p.ProductId)
			? null
			: $"product '{p.ProductId}' is not in the list of available products from config/products.yml. Available products: {m.Configuration.AvailableProducts}"
	).Describe("R41: Every product of a changelog entry is in products.yml (stage dependent: not checked at bundle or render).");

	public static Constraint<ChangelogEntryInConfiguration> EntryProductsAvailable { get; } = Constraints<ChangelogEntryInConfiguration>
		.Each(
			Products,
			(m, p) => m.Configuration.Configuration.ProductsConfiguration?.Available is { Count: > 0 } available &&
				!string.IsNullOrWhiteSpace(p.ProductId) && !available.Contains(Normalize(p.ProductId), StringComparer.OrdinalIgnoreCase)
				? $"product '{p.ProductId}' is not in products.available of changelog.yml ({string.Join(", ", available)}); 'products.available' restricts the products an entry may use."
				: null
		)
		.WithSeverity(Severity.Warning)
		.Describe("R6: Every product of a changelog entry is in the configuration's products.available, when that list is set.");

	public static Constraint<ChangelogEntryInConfiguration> EntryTypeAllowed
	{
		get;
	} = Constraints<ChangelogEntryInConfiguration>.EachOneOf<string>(
		m => m.Entry.IsMarker || m.Entry.Type == ChangelogEntryType.Invalid ? [] : [m.Entry.Type.ToStringFast(true)],
		m => m.Configuration.Configuration.Types,
		StringComparer.Ordinal,
		describe: (type, types) => $"type '{type}' is not a configured type; expected one of: {string.Join(", ", types)}"
	).Describe(
		"R37: The type of a changelog entry is one of the configuration's types (case-sensitive at create, the enum at validate, unchecked at bundle)."
	);

	public static Constraint<ChangelogEntryInConfiguration> EntrySubtypeAllowed
	{
		get;
	} = Constraints<ChangelogEntryInConfiguration>.EachOneOf<string>(
		m => m.Entry.Subtype is { } subtype ? [subtype.ToStringFast(true)] : [],
		m => m.Configuration.Configuration.SubTypes,
		StringComparer.Ordinal,
		describe: (subtype, subtypes) => $"subtype '{subtype}' is not a configured subtype; expected one of: {string.Join(", ", subtypes)}"
	).Describe(
		"R39: The subtype of a changelog entry is one of the configuration's subtypes (case-sensitive at create, insensitive at validate)."
	);

	public static Constraint<ChangelogEntryInConfiguration> EntryLifecycleAllowed
	{
		get;
	} = Constraints<ChangelogEntryInConfiguration>.Each(
		Products,
		(m, p) => p.Lifecycle is { } l && !m.Configuration.Configuration.Lifecycles.Contains(l)
			? $"lifecycle '{p.Lifecycle?.ToStringFast(true)}' of product '{p.ProductId}' is not a configured lifecycle; expected one of: {string.Join(", ", m.Configuration.Configuration.Lifecycles.Select(x => x.ToStringFast(true)))}"
			: null
	).Describe("R43: The lifecycle of every product of a changelog entry is one of the configuration's lifecycles.");

	public static Constraint<ChangelogEntryInConfiguration> EntryAreasAllowed
	{
		get;
	} = Constraints<ChangelogEntryInConfiguration>.EachOneOf<string>(
		m => m.Configuration.Configuration.Areas is { Count: > 0 } ? m.Entry.Areas ?? [] : [],
		m => m.Configuration.Configuration.Areas ?? [],
		StringComparer.Ordinal,
		describe: (area, areas) => $"area '{area}' is not a configured area; expected one of: {string.Join(", ", areas)}"
	).Describe(
		"R44: Every area of a changelog entry is one of the configuration's areas (case-sensitive at create, insensitive at validate)."
	);

	private static IReadOnlyList<ProductReference> Products(ChangelogEntryInConfiguration m) => m.Entry.Products ?? [];

	private static string Normalize(string productId) => ProductNames.Normalize(productId);
}
