// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Xml.Linq;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.IO.Navigation;

namespace Documentation.Assembler.Building;

public class SitemapBuilder(
	IReadOnlyCollection<INavigationItem> navigationItems,
	IFileSystem fileSystem,
	IDirectoryInfo outputFolder
)
{
	private static readonly Uri BaseUri = new("https://www.elastic.co");

	public void Generate()
	{
		var flattenedNavigationItems = GetNavigationItems(navigationItems);

		var doc = new XDocument()
		{
			Declaration = new XDeclaration("1.0", "utf-8", "yes"),
		};

		var currentDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz");
		var root = new XElement(
				"urlset",
				new XAttribute("xlmns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
				flattenedNavigationItems
					.OfType<FileNavigationItem>()
					.Select(n => n.Model.Url)
					.Distinct()
					.Select(u => new Uri(BaseUri, u))
					.Select(u => new XElement("url", [
						new XElement("loc", u),
						new XElement("lastmod", currentDate)
					]))
			);

		doc.Add(root);

		using var fileStream = fileSystem.File.Create(Path.Combine(outputFolder.ToString() ?? string.Empty, "docs", "sitemap.xml"));
		doc.Save(fileStream);
	}

	private static IReadOnlyCollection<INavigationItem> GetNavigationItems(IReadOnlyCollection<INavigationItem> items)
	{
		var result = new List<INavigationItem>();
		foreach (var item in items)
		{
			switch (item)
			{
				case FileNavigationItem file:
					result.Add(file);
					break;
				case DocumentationGroup group:
					result.AddRange(GetNavigationItems(group.NavigationItems));
					break;
			}
		}
		return result;
	}
}
