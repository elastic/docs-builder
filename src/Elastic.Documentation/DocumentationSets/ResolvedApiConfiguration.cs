// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Equality;
using Generator.Equals;

namespace Elastic.Documentation.DocumentationSets;

/// <summary>An <c>api</c> entry of <c>docset.yml</c>, validated and bound to its <see cref="Catalog.Product"/>.</summary>
/// <remarks>Produced when a documentation set is resolved. Its members stay public <c>init</c>: other test projects build them directly.</remarks>
[Equatable]
public sealed partial record ResolvedApiConfiguration
{
	public required string ProductKey { get; init; }

	/// <summary>The <c>products.yml</c> product this API key binds to.</summary>
	[DefaultEquality]
	public required Product Product { get; init; }

	/// <summary>
	/// The basename (including extension) of the declared <c>spec:</c> value, e.g.
	/// <c>elasticsearch-openapi.json</c>. This is the <c>&lt;spec-name&gt;</c> segment used to
	/// resolve the remote version index at the root <c>index.json</c> keyed by
	/// <c>&lt;org/repo&gt;</c> and this basename,
	/// regardless of whether <see cref="LocalSpecFile"/> is present.
	/// </summary>
	public required string SpecFileName { get; init; }

	/// <summary>
	/// Local override for the current OpenAPI specification file, present only when a file exists
	/// on disk at the path declared via <c>spec:</c>. Null means the current version resolves
	/// remotely through the product's version index — this is expected, not an error, for any
	/// docset that does not carry the spec file locally.
	/// </summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public IFileInfo? LocalSpecFile { get; init; }

	/// <summary>
	/// Optional <c>org/repo</c> override from <c>repository:</c>, used instead of the current
	/// checkout's GitHub remote to look up this API's entry in the root version index. Null means
	/// use the checkout's own remote (the common case: a repo consuming its own published spec).
	/// </summary>
	public string? Repository { get; init; }

	/// <summary>
	/// Explicit child pages declared via <c>children:</c>, resolved to files under <c>api/&lt;key&gt;/</c>,
	/// in declared order.
	/// </summary>
	[OrderedEquality(typeof(FileSystemInfoLocationComparer))]
	public IReadOnlyList<IFileInfo> Children { get; init; } = [];

	/// <summary>
	/// The <c>api/&lt;key&gt;/</c> directory for this product, whether or not it exists yet.
	/// Supplemental <c>op-*.md</c> / <c>tag-*.md</c> files are discovered from here.
	/// </summary>
	[CustomEquality(typeof(FileSystemInfoLocationComparer))]
	public IDirectoryInfo? ApiContentDirectory { get; init; }

	/// <summary>
	/// Normalized catalog categories (<c>ece</c>, <c>ess</c>, <c>self</c>, <c>serverless</c>).
	/// Empty when the API is unclassified and appears only under All.
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string> CatalogCategories { get; init; } = [];

	/// <summary>
	/// Whether <paramref name="fileName"/> is an auto-discovered supplemental file
	/// (<c>op-*.md</c> or <c>tag-*.md</c>), including version-suffixed names.
	/// </summary>
	public static bool IsSupplementalFileName(string fileName)
	{
		var name = Path.GetFileName(fileName);
		return name.StartsWith("op-", StringComparison.OrdinalIgnoreCase) || name.StartsWith("tag-", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Markdown paths that must not be rendered by the normal HTML pipeline:
	/// explicit <c>children:</c> pages and convention supplemental files.
	/// </summary>
	public IEnumerable<string> GetMarkdownPathsToExclude(string documentationSourceDirectoryFullName)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var file in Children)
		{
			var relative = ToRelativeMarkdownPath(file, documentationSourceDirectoryFullName);
			if (seen.Add(relative))
				yield return relative;
		}

		foreach (var file in EnumerateApiMarkdownFiles())
		{
			if (!IsSupplementalFileName(file.Name))
				continue;
			var relative = ToRelativeMarkdownPath(file, documentationSourceDirectoryFullName);
			if (seen.Add(relative))
				yield return relative;
		}
	}

	/// <summary>Top-level Markdown files under <see cref="ApiContentDirectory"/>, when the folder exists.</summary>
	public IEnumerable<IFileInfo> EnumerateApiMarkdownFiles()
	{
		if (ApiContentDirectory is not { } dir)
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
