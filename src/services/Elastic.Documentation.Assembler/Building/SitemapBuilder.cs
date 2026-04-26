// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Xml.Linq;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Markdown.Extensions.DetectionRules;

namespace Elastic.Documentation.Assembler.Building;

public static class SitemapBuilder
{
	private static readonly Uri BaseUri = new("https://www.elastic.co");

	/// <summary>Generates sitemap.xml with per-URL last_updated dates.</summary>
	public static void Generate(
		IReadOnlyDictionary<string, DateTimeOffset> entries,
		IFileSystem fileSystem,
		IDirectoryInfo outputFolder
	)
	{
		var doc = new XDocument
		{
			Declaration = new XDeclaration("1.0", "utf-8", "yes")
		};

		XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		var root = new XElement(
			ns + "urlset",
			new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
			entries
				.OrderBy(e => e.Key, StringComparer.Ordinal)
				.Select(e => new XElement(ns + "url", [
					new XElement(ns + "loc", new Uri(BaseUri, e.Key)),
					new XElement(ns + "lastmod", e.Value.ToString("o", CultureInfo.InvariantCulture))
				]))
		);

		doc.Add(root);

		if (!outputFolder.Exists)
			_ = fileSystem.Directory.CreateDirectory(outputFolder.FullName);

		using var fileStream = fileSystem.File.Create(fileSystem.Path.Join(outputFolder.FullName, "sitemap.xml"));
		doc.Save(fileStream);
	}
}

/// <summary>Extracts URLs from navigation items for sitemap generation.</summary>
public static class SitemapNavigationHelper
{
	public static IEnumerable<INavigationItem> Flatten(INavigationItem item) =>
		item switch
		{
			ILeafNavigationItem<CrossLinkModel> => [],
			ILeafNavigationItem<DetectionRuleFile> => [],
			ILeafNavigationItem<INavigationModel> { Hidden: true } => [],
			ILeafNavigationItem<INavigationModel> file => [file],
			INodeNavigationItem<INavigationModel, INavigationItem> { Hidden: true } => [],
			INodeNavigationItem<INavigationModel, INavigationItem> group =>
				group.NavigationItems.SelectMany(Flatten).Append(group),
			_ => []
		};
}
