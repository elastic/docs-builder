// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Xml.Linq;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Markdown.Extensions.DetectionRules;

namespace Elastic.Documentation.Assembler.Building;

/// <summary>
/// Where the sitemap goes: <see cref="Folder"/> is served at <see cref="PathPrefix"/> on <see cref="Host"/>, the host the
/// environment deploys to. The index points to its files there; the pages keep their canonical host.
/// </summary>
public record SitemapOutput(IFileSystem FileSystem, IDirectoryInfo Folder, string? PathPrefix, Uri Host)
{
	private string ApiRoot => ApiUrlBuilder.ApiRoot($"/{PathPrefix?.Trim('/')}");

	public static SitemapOutput For(AssembleContext context) =>
		new(
			context.WriteFileSystem,
			context.OutputWithPathPrefixDirectory,
			context.Environment.PathPrefix,
			new Uri(context.Environment.Uri)
		);

	/// <summary>Whether a site path is an API Explorer page: the API hub or a page below it.</summary>
	public bool IsApiUrl(string path) =>
		path.StartsWith(ApiRoot, StringComparison.Ordinal) && (path.Length == ApiRoot.Length || path[ApiRoot.Length] == '/');

	public string PathOf(string fileName) => FileSystem.Path.Join(Folder.FullName, fileName);
}

/// <summary>One sitemap file below the index.</summary>
public record SitemapFile(string Name, int EntryCount, long FileSizeBytes);

/// <summary>
/// Writes <c>sitemap.xml</c> as a sitemap index over one file of narrative pages and one of API pages, so the URL registered
/// with search engines stays the same while each file keeps under the protocol limits.
/// </summary>
public static class SitemapBuilder
{
	private const string IndexFileName = "sitemap.xml";
	private const string DocsFileName = "sitemap-docs.xml";
	private const string ApiFileName = "sitemap-api.xml";

	public const int MaxEntries = 50_000;
	public const int WarningEntryThreshold = 40_000;
	public const long MaxFileSizeBytes = 50L * 1024 * 1024;
	public const long WarningFileSizeBytes = 40L * 1024 * 1024;

	private static readonly Uri BaseUri = new("https://www.elastic.co");
	private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

	/// <summary>
	/// Writes the files of <paramref name="entries"/> and the index, with per-URL last-modified dates. The API file is written
	/// only when there are API entries, and the index lists it whenever it exists: <c>assembler sitemap</c> passes only the
	/// narrative pages of the search index, which holds operations rather than pages, and keeps the API file of the build.
	/// API pages only reach the build's entries where the <c>assembler-api-explorer</c> flag renders them.
	/// </summary>
	public static IReadOnlyList<SitemapFile> Generate(
		IReadOnlyDictionary<string, DateTimeOffset> entries,
		SitemapOutput output,
		IDiagnosticsCollector collector
	)
	{
		var byKind = entries.ToLookup(e => output.IsApiUrl(e.Key));

		if (!output.Folder.Exists)
			_ = output.FileSystem.Directory.CreateDirectory(output.Folder.FullName);

		List<SitemapFile> files = [WriteUrlSet(DocsFileName, [.. byKind[false]], output, collector)];
		if (byKind[true].Any())
			files.Add(WriteUrlSet(ApiFileName, [.. byKind[true]], output, collector));

		var listed = output.FileSystem.File.Exists(output.PathOf(ApiFileName)) ? [DocsFileName, ApiFileName] : new[] { DocsFileName };
		var prefix = string.IsNullOrEmpty(output.PathPrefix) ? string.Empty : $"{output.PathPrefix.Trim('/')}/";
		var index = new XElement(
			Ns + "sitemapindex",
			listed.Select(name => new XElement(Ns + "sitemap", new XElement(Ns + "loc", new Uri(output.Host, prefix + name))))
		);
		using var indexXml = Serialize(index);
		Write(indexXml, output, IndexFileName);
		return files;
	}

	private static SitemapFile WriteUrlSet(
		string name,
		IReadOnlyCollection<KeyValuePair<string, DateTimeOffset>> entries,
		SitemapOutput output,
		IDiagnosticsCollector collector
	)
	{
		if (entries.Count > MaxEntries)
			throw new InvalidOperationException(
				$"{name} contains {entries.Count:N0} URLs, which exceeds the sitemap protocol limit of {MaxEntries:N0}. Split it into more files below the index."
			);
		if (entries.Count >= WarningEntryThreshold)
			collector.EmitGlobalWarning($"{name} has {entries.Count:N0} entries, approaching the {MaxEntries:N0} URL protocol limit.");

		var urlSet = new XElement(
			Ns + "urlset",
			entries.OrderBy(e => e.Key, StringComparer.Ordinal).Select(
				e => new XElement(
					Ns + "url",
					new XElement(Ns + "loc", new Uri(BaseUri, e.Key)),
					new XElement(Ns + "lastmod", e.Value.ToString("o", CultureInfo.InvariantCulture))
				)
			)
		);
		// checked before the file is replaced, so a refresh that fails keeps the previous file
		using var xml = Serialize(urlSet);
		var size = xml.Length;
		if (size > MaxFileSizeBytes)
			throw new InvalidOperationException(
				$"{name} is {size / (1024.0 * 1024.0):F1} MB, which exceeds the sitemap protocol limit of 50 MB. Split it into more files below the index."
			);
		if (size >= WarningFileSizeBytes)
			collector.EmitGlobalWarning($"{name} is {size / (1024.0 * 1024.0):F1} MB, approaching the 50 MB protocol limit.");

		Write(xml, output, name);
		return new SitemapFile(name, entries.Count, size);
	}

	private static MemoryStream Serialize(XElement root)
	{
		var buffer = new MemoryStream();
		new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).Save(buffer);
		buffer.Position = 0;
		return buffer;
	}

	private static void Write(MemoryStream xml, SitemapOutput output, string name)
	{
		using var file = output.FileSystem.File.Create(output.PathOf(name));
		xml.CopyTo(file);
	}
}

/// <summary>Extracts URLs from navigation items for sitemap generation.</summary>
public static class SitemapNavigationHelper
{
	public static IEnumerable<INavigationItem> Flatten(INavigationItem item) => item switch
	{
		ILeafNavigationItem<CrossLinkModel> => [],
		ILeafNavigationItem<DetectionRuleFile> => [],
		ILeafNavigationItem<INavigationModel> { InSitemap: false } => [],
		// a link to a heading inside a page is not a page of its own
		ILeafNavigationItem<INavigationModel> { Url: var url } when url.Contains('#') => [],
		ILeafNavigationItem<INavigationModel> file => [file],
		INodeNavigationItem<INavigationModel, INavigationItem> { InSitemap: false } => [],
		INodeNavigationItem<INavigationModel, INavigationItem> group => group.NavigationItems.SelectMany(Flatten).Append(group),
		_ => []
	};
}
