// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

[YamlSerializable]
public class TableOfContentsFile
{
	[YamlMember(Alias = "project")]
	public string? Project { get; set; }

	[YamlMember(Alias = "toc")]
	public TableOfContents TableOfContents { get; set; } = [];

	/// <summary>
	/// When <c>true</c>, this table of contents is rendered as an island in the navigation tree.
	/// In isolated builds the root docset is never an island (it has no parent); in assembled builds
	/// the node is re-parented under <c>SiteNavigation</c> and the flag takes effect.
	/// </summary>
	[YamlMember(Alias = "island")]
	public bool Island { get; set; }

	/// <summary>
	/// Set of diagnostic hint types to suppress. Deserialized directly from YAML list of strings.
	/// Valid values: "DeepLinkingVirtualFile", "FolderFileNameMismatch"
	/// </summary>
	[YamlMember(Alias = "suppress")]
	public HashSet<HintType> SuppressDiagnostics { get; set; } = [];

	public static TableOfContentsFile Deserialize(string json) =>
		ConfigurationFileProvider.Deserializer.Deserialize<TableOfContentsFile>(json);
}

public class TableOfContents : List<ITableOfContentsItem>
{
	public TableOfContents() { }

	public TableOfContents(IEnumerable<ITableOfContentsItem> items) : base(items) { }
}
