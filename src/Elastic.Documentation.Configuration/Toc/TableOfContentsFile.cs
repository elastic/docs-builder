// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

public class TableOfContentsFile
{
	[YamlPropertyName("project")]
	public string? Project { get; set; }

	[YamlPropertyName("toc")]
	public TableOfContents TableOfContents { get; set; } = [];

	/// <summary>
	/// When <c>true</c>, this table of contents is rendered as an island in the navigation tree.
	/// In isolated builds the root docset is never an island (it has no parent); in assembled builds
	/// the node is re-parented under <c>SiteNavigation</c> and the flag takes effect.
	/// </summary>
	[YamlPropertyName("island")]
	public bool Island { get; set; }

	/// <summary>
	/// Set of diagnostic hint types to suppress for the entries in this table of contents.
	/// Deserialized directly from YAML list of strings.
	/// Valid values: "DeepLinkingVirtualFile", "FolderFileNameMismatch", "AutolinkElasticCoDocs"
	/// (the latter is only honoured from <c>docset.yml</c>).
	/// </summary>
	[YamlPropertyName("suppress")]
	public HashSet<HintType> SuppressDiagnostics { get; set; } = [];

	/// <summary>
	/// Optional name of a <c>cta</c> template (declared in <c>docset.yml</c>) applied to every page
	/// listed in this navigation file unless the page selects one explicitly via frontmatter.
	/// Nested <c>toc.yml</c> files may override the value inherited from a parent navigation file.
	/// </summary>
	[YamlPropertyName("default_cta")]
	public string? DefaultCta { get; set; }

	public static TableOfContentsFile Deserialize(string json) => ConfigurationFileProvider.Deserialize<TableOfContentsFile>(json);

	/// <summary>
	/// Deserializes a <c>toc.yml</c>, reporting unknown keys to <paramref name="collector"/> instead of
	/// silently skipping them.
	/// </summary>
	public static TableOfContentsFile Deserialize(string json, IDiagnosticsCollector collector, string sourceName) =>
		ConfigurationFileProvider.Deserialize<TableOfContentsFile>(json, collector, sourceName);
}

public class TableOfContents : List<ITableOfContentsItem>
{
	public TableOfContents() { }

	public TableOfContents(IEnumerable<ITableOfContentsItem> items) : base(items) { }
}
