// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.DocumentationSets;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>
/// Loading the same input twice yields equal aggregates, and a change anywhere in an aggregate makes it unequal.
/// This is what value equality on the domain records buys: caches, change detection and property tests can compare
/// aggregates directly instead of projecting them.
/// </summary>
public class AggregateEqualityTests
{
	[Test]
	public void GlobalCatalog_LoadedTwice_IsEqual() =>
		CatalogLoader.Load(BaselineProvider()).OrThrow().Should().Be(CatalogLoader.Load(BaselineProvider()).OrThrow());

	[Test]
	public void GlobalCatalog_OneProductRenamed_IsNotEqual()
	{
		var catalog = CatalogLoader.Load(BaselineProvider()).OrThrow();
		var (id, product) = catalog.Products.Products.First();
		var renamed = catalog with
		{
			Products = catalog.Products with
			{
				Products = catalog
					.Products
					.Products
					.Select(kv => kv.Key == id ? new(kv.Key, product with { DisplayName = product.DisplayName + " (renamed)" }) : kv)
					.ToFrozenDictionary()
			}
		};

		renamed.Should().NotBe(catalog);
	}

	[Test]
	public void SearchTuning_LoadedTwice_IsEqual() =>
		SearchTuningLoader.Load(BaselineProvider()).OrThrow().Should().Be(SearchTuningLoader.Load(BaselineProvider()).OrThrow());

	[Test]
	public void RelatedLearning_LoadedTwice_IsEqual() =>
		RelatedLearningLoader.Load(BaselineProvider()).OrThrow().Should().Be(RelatedLearningLoader.Load(BaselineProvider()).OrThrow());

	private static IReadOnlyList<TocItem> LoadBaselineToc()
	{
		var fs = DocumentationSetGoldenTests.CreateDocsTree();
		var docsetFile = fs.FileInfo.New(Path.Join(DocumentationSetGoldenTests.DocsRoot, "_docset.yml"));
		return DocumentationSetFile.LoadAndResolve(new CapturingDiagnosticsCollector(), docsetFile).TableOfContents;
	}

	[Test]
	public void TableOfContents_LoadedTwice_IsEqual() => LoadBaselineToc().Should().Equal(LoadBaselineToc());

	[Test]
	public void TableOfContents_OneNestedFileHidden_IsNotEqual()
	{
		var toc = LoadBaselineToc();
		var nested = toc.OfCase<IsolatedTableOfContentsRef>().First();
		var file = nested.Children.OfCase<FileRef>().First();
		var changed = toc.Select(
			i => i.Value == nested
				? new TocItem(nested with
				{
					Children = [.. nested.Children.Select(c => c.Value == file ? new TocItem(file with { Hidden = !file.Hidden }) : c)]
				})
				: i
		).ToList();

		changed.Should().NotEqual(toc);
	}
}
