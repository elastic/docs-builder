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

// TODO rewrite as real exporter
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

		var doc = new XDocument
		{
			Declaration = new XDeclaration("1.0", "utf-8", "yes")
		};

		XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		var currentDate = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
		var root = new XElement(
			ns + "urlset",
			new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
			flattenedNavigationItems
				.Select(n => n switch
				{
					INodeNavigationItem<INavigationModel, INavigationItem> group => (group.Url, NavigationItem: group),
					ILeafNavigationItem<INavigationModel> file => (file.Url, NavigationItem: file as INavigationItem),
					_ => throw new Exception($"{nameof(SitemapBuilder)}.{nameof(Generate)}: Unhandled navigation item type: {n.GetType()}")
				})
				.Select(n => n.Url)
				.Distinct()
				.Select(u => new Uri(BaseUri, u))
				.Select(u => new XElement(ns + "url", [
					new XElement(ns + "loc", u),
					new XElement(ns + "lastmod", currentDate)
				]))
		);

		doc.Add(root);

		using var fileStream = fileSystem.File.Create(fileSystem.Path.Combine(outputFolder.FullName, "sitemap.xml"));
		doc.Save(fileStream);
	}

	private static IReadOnlyCollection<INavigationItem> GetNavigationItems(IReadOnlyCollection<INavigationItem> items)
	{
		var result = new List<INavigationItem>();
		foreach (var item in items)
		{
			switch (item)
			{
				case ILeafNavigationItem<CrossLinkModel>:
				case ILeafNavigationItem<DetectionRuleFile>:
				case ILeafNavigationItem<INavigationModel> { Hidden: true }:
					continue;
				case ILeafNavigationItem<INavigationModel> file:
					result.Add(file);
					break;
				case INodeNavigationItem<INavigationModel, INavigationItem> group:
					if (item.Hidden)
						continue;

					result.AddRange(GetNavigationItems(group.NavigationItems));
					result.Add(group);
					break;
				default:
					throw new Exception($"{nameof(SitemapBuilder)}.{nameof(GetNavigationItems)}: Unhandled navigation item type: {item.GetType()}");
			}
		}

		return result;
	}
}
