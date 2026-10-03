// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.DocumentationSets;

namespace Elastic.Documentation.Entities.Tests;

/// <summary>The rules the investigation lists as unchecked, run on the configuration docs-builder ships and on its own docs: real data, no generator.</summary>
public class ShippedConfigurationEntityTests
{
	[Test]
	public void Shipped_assembly_breaks_no_entity_rule() =>
		EntityRules.Assembly.Validate(Support.ShippedAssembly).Select(v => v.Message).Should().BeEmpty();

	[Test]
	[Arguments("navigation.yml")]
	[Arguments("navigation_preview.yml")]
	public void Shipped_navigation_mounts_only_repositories_the_assembly_publishes(string file)
	{
		var assembly = Support.ShippedAssembly;
		var yaml = File.ReadAllText(Path.Join(Support.SolutionDirectory, "config", file));
		var navigation = SiteNavigationLoader.ForAssembly(SiteNavigationLoader.Deserialize(yaml), assembly, skipPrivateRepositories: false);

		EntityRules.Mounts.Validate(new NavigationInAssembly(navigation, assembly)).Select(v => v.Message).Should().BeEmpty();
	}

	[Test]
	public void The_docs_docset_lists_no_path_twice_and_names_only_products_the_catalog_has()
	{
		var fileSystem = new FileSystem();
		var docsetFile = fileSystem.FileInfo.New(Path.Join(Support.SolutionDirectory, "docs", "_docset.yml"));
		var definition = DocumentationSetLoader.Load(docsetFile, docsetFile.Directory!, fileSystem).Value!;
		var catalog = CatalogLoader.Load(Support.ShippedProvider).OrThrow().Products;

		EntityRules.Toc.Validate(definition).Select(v => v.Message).Should().BeEmpty();
		EntityRules.Products.Validate(new DocumentationSetInCatalog(definition, catalog)).Select(v => v.Message).Should().BeEmpty();
	}
}
