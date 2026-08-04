// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Changelog.Backfill.Inventory;

/// <summary>
/// The hand-maintained census mapping the inventory stage consumes: where each product's
/// release notes live, plus products deliberately deferred (each with a reason). The census
/// turns this into an <see cref="InventoryDocument"/>, filling classification gaps so no
/// product silently disappears. Field values use the same kebab-case names the inventory
/// document serializes with (e.g. <c>published-history-found</c>, <c>not-adopted</c>).
/// </summary>
public sealed class InventorySourcesSeed
{
	/// <summary>One entry per known release-note source.</summary>
	public List<SeedSource> Sources { get; set; } = [];

	/// <summary>Products intentionally not mapped yet; each needs a reason so the deferral is auditable.</summary>
	public List<SeedUnmapped> Unmapped { get; set; } = [];

	private static readonly IDeserializer Deserializer =
		new StaticDeserializerBuilder(new InventorySeedYamlContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

	/// <summary>Parses a seed document from YAML. Malformed YAML throws; semantic problems are reported by the census.</summary>
	public static InventorySourcesSeed Deserialize(string yaml) =>
		Deserializer.Deserialize<InventorySourcesSeed>(yaml) ?? new InventorySourcesSeed();
}

/// <summary>One release-note source in the seed: a repository plus everything the census records about it.</summary>
public sealed class SeedSource
{
	/// <summary>The repository the release-note content lives in, as <c>owner/name</c> (e.g. <c>elastic/docs-content</c>).</summary>
	public string? Repository { get; set; }

	/// <summary>The git ref (branch, tag, or commit) the content is read at.</summary>
	public string? GitRef { get; set; }

	/// <summary>The docset the content belongs to, when the repository hosts more than one.</summary>
	public string? Docset { get; set; }

	/// <summary>Paths inside the repository where the release-note content lives.</summary>
	public List<string> Paths { get; set; } = [];

	/// <summary>The product IDs (as known to <c>products.yml</c>) this source produces release notes for.</summary>
	public List<string> Products { get; set; } = [];

	/// <summary>How the product names its releases: <c>semver</c>, <c>date</c>, or <c>monthly</c>.</summary>
	public string? TargetScheme { get; set; }

	/// <summary>Where hand-written history ends and live workflow data begins.</summary>
	public SeedCutoff? Cutoff { get; set; }

	/// <summary>Docset variable substitutions (like <c>{{es}}</c> → <c>Elasticsearch</c>) needed to expand the source text.</summary>
	public Dictionary<string, string> Substitutions { get; set; } = [];

	/// <summary>Known mappings from links as written in the source to the destinations they should resolve to.</summary>
	public Dictionary<string, string> LinkMappings { get; set; } = [];

	/// <summary>Repositories entries attribute changes to, as <c>owner/name</c>. Allowlist status is computed by the census, not recorded here.</summary>
	public List<string> AttributedRepositories { get; set; } = [];

	/// <summary>The repository to attribute content to when an entry does not say where it came from, as <c>owner/name</c>.</summary>
	public string? DefaultRepository { get; set; }

	/// <summary>The file-name pattern bundles for this source are expected to use, e.g. <c>{repo}-{target}.yaml</c>.</summary>
	public string? BundleFilenameConvention { get; set; }

	/// <summary>Adoption of the live changelog workflows: <c>not-adopted</c>, <c>partially-adopted</c>, or <c>fully-adopted</c>.</summary>
	public string? Adoption { get; set; }

	/// <summary>The census conclusion, e.g. <c>published-history-found</c> or <c>native-artifacts-found</c>.</summary>
	public string? Classification { get; set; }

	/// <summary>Open questions about this source that a human still needs to answer.</summary>
	public List<string> Unresolved { get; set; } = [];
}

/// <summary>A backfill boundary in the seed: everything on or after it is in scope.</summary>
public sealed class SeedCutoff
{
	/// <summary>Whether <see cref="Value"/> is a <c>version</c> or a <c>date</c>.</summary>
	public string? Kind { get; set; }

	/// <summary>The boundary itself, e.g. <c>9.0.0</c> or <c>2025-01-01</c>.</summary>
	public string? Value { get; set; }

	/// <summary>Optional free-text explanation of why this boundary was chosen.</summary>
	public string? Notes { get; set; }
}

/// <summary>A product deliberately left unmapped for now, with the reason recorded.</summary>
public sealed class SeedUnmapped
{
	/// <summary>The product ID as known to <c>products.yml</c>.</summary>
	public string? Product { get; set; }

	/// <summary>Why this product is not mapped yet — required, so the deferral is auditable.</summary>
	public string? Reason { get; set; }
}

[YamlStaticContext]
[YamlSerializable(typeof(InventorySourcesSeed))]
[YamlSerializable(typeof(SeedSource))]
[YamlSerializable(typeof(SeedCutoff))]
[YamlSerializable(typeof(SeedUnmapped))]
public partial class InventorySeedYamlContext;
