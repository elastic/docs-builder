// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Publishing;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// A single declared child page under <c>api/&lt;key&gt;/</c>, referenced by <c>children:</c>.
/// </summary>
[YamlSerializable]
internal class ApiEntryChildDto
{
	/// <summary>
	/// Path to a Markdown file, relative to <c>api/&lt;key&gt;/</c>.
	/// </summary>
	[YamlMember(Alias = "file")]
	public string? File { get; set; }
}

/// <summary>
/// The single strict entry for an <c>api: &lt;key&gt;</c> product, per the RFC schema:
/// <code>
/// api:
///   &lt;key&gt;:
///     - spec: &lt;path-or-filename&gt;
///       product: &lt;product-id&gt;
///       repository: &lt;org/repo&gt;
///       children:
///         - file: getting-started.md
/// </code>
/// <c>spec</c> and <c>product</c> are both required. <c>spec</c> serves two purposes at once: if a
/// file exists at that path relative to the docset, it overrides the current version for local
/// preview; regardless of whether it exists on disk, its basename is the <c>&lt;spec-name&gt;</c>
/// segment looked up in the root version index. <c>repository</c> is optional and only needed when
/// the spec-publishing repository differs from the current checkout's own GitHub remote.
/// </summary>
[YamlSerializable]
internal class ApiProductEntryDto
{
	/// <summary>
	/// Path to an OpenAPI specification file, relative to the docset. Required: its basename is
	/// used to resolve the remote version index even when no file exists at this path locally.
	/// </summary>
	[YamlMember(Alias = "spec")]
	public string? Spec { get; set; }

	/// <summary>
	/// Required product id. Must match a key in <c>products.yml</c> and binds this API to that
	/// product's versioning system.
	/// </summary>
	[YamlMember(Alias = "product")]
	public string? Product { get; set; }

	/// <summary>
	/// Optional <c>org/repo</c> override for resolving this API's entry in the root version index.
	/// Required whenever the spec-publishing repository differs from the current checkout's own
	/// GitHub remote (e.g. Elasticsearch's docs build from <c>elastic/elasticsearch</c>, but its
	/// OpenAPI spec is published from <c>elastic/elasticsearch-specification</c>). When omitted, the
	/// current checkout's GitHub remote is used instead.
	/// </summary>
	[YamlMember(Alias = "repository")]
	public string? Repository { get; set; }

	/// <summary>
	/// Explicit child pages rendered under <c>api/&lt;key&gt;/</c>, in declared order.
	/// </summary>
	[YamlMember(Alias = "children")]
	public List<ApiEntryChildDto> Children { get; set; } = [];

	/// <summary>
	/// Optional catalog grouping. Categories identify filter chips only; they do not
	/// claim versioned availability.
	/// </summary>
	[YamlMember(Alias = "catalog")]
	public ApiCatalogDto? Catalog { get; set; }

	/// <summary>
	/// 1-based line of this entry's mapping start in the source YAML. Populated by
	/// <see cref="ApiConfigurationConverter"/>; used to attribute diagnostics that have no more
	/// specific location, such as a missing <c>product:</c> key.
	/// </summary>
	[YamlIgnore]
	public int? Line { get; set; }

	/// <summary>1-based column counterpart to <see cref="Line"/>.</summary>
	[YamlIgnore]
	public int? Column { get; set; }

	/// <summary>
	/// Source location of the <c>product:</c> value specifically, when present. Used to attribute
	/// an unknown-product diagnostic to the exact value rather than the whole entry.
	/// </summary>
	[YamlIgnore]
	public int? ProductLine { get; set; }

	/// <summary>1-based column counterpart to <see cref="ProductLine"/>.</summary>
	[YamlIgnore]
	public int? ProductColumn { get; set; }

	/// <summary>
	/// Source location of the <c>spec:</c> value specifically, when present. Used to attribute a
	/// missing- or invalid-spec diagnostic to the exact value rather than the whole entry.
	/// </summary>
	[YamlIgnore]
	public int? SpecLine { get; set; }

	/// <summary>1-based column counterpart to <see cref="SpecLine"/>.</summary>
	[YamlIgnore]
	public int? SpecColumn { get; set; }

	/// <summary>
	/// Source location of the <c>repository:</c> value specifically, when present. Used to attribute
	/// a malformed-repository diagnostic to the exact value rather than the whole entry.
	/// </summary>
	[YamlIgnore]
	public int? RepositoryLine { get; set; }

	/// <summary>1-based column counterpart to <see cref="RepositoryLine"/>.</summary>
	[YamlIgnore]
	public int? RepositoryColumn { get; set; }

	public bool HasSpec => !string.IsNullOrWhiteSpace(Spec);
	public bool HasProduct => !string.IsNullOrWhiteSpace(Product);
}

/// <summary>
/// Category-only catalog metadata under <c>catalog:</c> on an API entry.
/// </summary>
[YamlSerializable]
internal class ApiCatalogDto
{
	[YamlMember(Alias = "categories")]
	public List<string> Categories { get; set; } = [];

	[YamlIgnore]
	public int? Line { get; set; }

	[YamlIgnore]
	public int? Column { get; set; }
}

/// <summary>
/// The YAML sequence bound to each <c>api: &lt;key&gt;</c> entry. The RFC schema always contains
/// exactly one <see cref="ApiProductEntryDto"/>; the sequence wrapper mirrors the wire format.
/// </summary>
[YamlSerializable]
internal class ApiProductSequenceDto
{
	public List<ApiProductEntryDto> Entries { get; set; } = [];

	/// <summary>
	/// Structural shape check only: exactly one entry is present. Product validity and spec/child
	/// resolution are checked separately so callers can attribute precise diagnostics.
	/// </summary>
	public bool IsValid => Entries.Count == 1;

	public ApiProductEntryDto? SingleEntry => Entries.Count == 1 ? Entries[0] : null;
}

/// <summary>The files an API configuration's content directory holds on disk.</summary>
public static class ResolvedApiConfigurationFiles
{
	/// <summary>
	/// Markdown paths that must not be rendered by the normal HTML pipeline:
	/// explicit <c>children:</c> pages and convention supplemental files.
	/// </summary>
	public static IEnumerable<string> GetMarkdownPathsToExclude(
		this ResolvedApiConfiguration api,
		string documentationSourceDirectoryFullName
	)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var file in api.Children)
		{
			var relative = ToRelativeMarkdownPath(file, documentationSourceDirectoryFullName);
			if (seen.Add(relative))
				yield return relative;
		}

		foreach (var file in api.EnumerateApiMarkdownFiles())
		{
			if (!ResolvedApiConfiguration.IsSupplementalFileName(file.Name))
				continue;
			var relative = ToRelativeMarkdownPath(file, documentationSourceDirectoryFullName);
			if (seen.Add(relative))
				yield return relative;
		}
	}

	/// <summary>Top-level Markdown files under <see cref="ApiContentDirectory"/>, when the folder exists.</summary>
	public static IEnumerable<IFileInfo> EnumerateApiMarkdownFiles(this ResolvedApiConfiguration api)
	{
		if (api.ApiContentDirectory is not { } dir)
			yield break;

		dir.Refresh();
		if (!dir.Exists)
			yield break;

		foreach (var file in dir.EnumerateFiles("*.md"))
			yield return file;
	}

	private static string ToRelativeMarkdownPath(IFileInfo file, string documentationSourceDirectoryFullName) =>
		Path.GetRelativePath(documentationSourceDirectoryFullName, file.FullName).Replace(Path.DirectorySeparatorChar, '/');
}
