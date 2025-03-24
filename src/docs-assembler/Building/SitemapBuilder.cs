// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Xml.Linq;
using Elastic.Markdown.IO.Navigation;

namespace Documentation.Assembler.Building;

public class SitemapBuilder(IReadOnlyCollection<INavigationItem> navigationItems, IFileSystem fileSystem, IDirectoryInfo outputFolder)
{
	private static readonly Uri BaseUri = new("https://www.elastic.co");
	private readonly IReadOnlyCollection<INavigationItem> _navigationItems = navigationItems;
	private readonly IFileSystem _fileSystem = fileSystem;
	private readonly IDirectoryInfo _outputFolder = outputFolder;

	public void Generate()
	{
		var flattenedNavigationItems = GetNavigationItems(_navigationItems);

		var doc = new XDocument()
		{
			Declaration = new XDeclaration("1.0", "utf-8", "yes"),
		};

		var root = new XElement(
				"urlset",
				new XAttribute("xlmns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
				flattenedNavigationItems
					.OfType<FileNavigationItem>()
					.Select(n => n.File.Url)
					.Distinct()
					.Select(u => new Uri(BaseUri, u))
					.Select(u => new XElement("url", new XElement("loc", u)))
			);

		doc.Add(root);

		using var fileStream = _fileSystem.File.Create(Path.Combine(_outputFolder.ToString() ?? string.Empty, "sitemap.xml"));
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
				case GroupNavigationItem group:
					result.AddRange(GetNavigationItems(group.Group.NavigationItems));
					break;
			}
		}
		return result;
	}
}