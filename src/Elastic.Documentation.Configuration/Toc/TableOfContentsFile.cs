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
