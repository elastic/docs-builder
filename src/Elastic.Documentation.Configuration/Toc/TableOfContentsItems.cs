// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// Sentinel produced when a bare scalar (e.g. <c>- toc.yml</c>) is used as a toc entry.
/// The converter consumes the token to prevent an infinite parse loop; this ref carries the
/// raw value so <see cref="DocumentationSetFile"/> can emit a clear diagnostic.
/// </summary>
internal record InvalidTocItemRef(string RawValue) : ITableOfContentsItem
{
	public string PathRelativeToDocumentationSet => RawValue;
	public string PathRelativeToContainer => RawValue;
	public string Context => "";
}

/// <summary>Frontmatter value for the <c>listing</c> key — both shorthand (<c>listing: group-name</c>)
/// and full form (<c>listing: {group: group-name}</c>) parse to this.</summary>
public class ListingFrontMatter
{
	/// <summary>The group key this page belongs to inside its listing.</summary>
	public string? Group { get; set; }
}
