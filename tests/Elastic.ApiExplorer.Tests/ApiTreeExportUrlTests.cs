// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Search.Contract;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

/// <summary>Every URL the search index stores for an API document must be a page, or a redirect to one, in each tree the document lists.</summary>
public class ApiTreeExportUrlTests
{
	[Test]
	public async Task ExportDocuments_MultiVersionFixture_EveryUrlIsAPageOfEveryListedTree()
	{
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			TestHelpers.CreateBuildContext(urlPathPrefix: "/docs"),
			NoopMarkdownStringRenderer.Instance
		);
		var eight = await Fixture();
		_ = eight.Paths.Remove("/{index}/_bulk");
		foreach (var operation in eight.Paths["/{index}/_search"].Operations!.Values)
			operation.Summary = "Search the v8 way";
		(string Moniker, OpenApiDocument Spec)[] trees = [("main", await Fixture()), ("9", await Fixture()), ("8", eight)];
		var pages = trees.ToDictionary(t => t.Moniker, t => PageUrls(generator, t.Moniker, t.Spec));
		var exporter = new OpenApiDocumentExporter(
			TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9, currentMinor: 5, patch: 6)
		);

		var docs = ApiTreeCollapse.Collapse(
			trees.SelectMany(t => exporter.ConvertToDocuments(new IndexedTree("elasticsearch", "elasticsearch", t.Moniker, t.Spec))),
			ApiVersions.DefaultTree(trees.Select(t => t.Moniker))!
		).ToList();

		docs.Should().Contain(d => d.Api!.Trees.Length == 3, "the fixture shares most operations across every tree");
		docs.Should().Contain(d => d.Api!.Trees.Length == 1 && d.Api.Trees[0].Tree == "8", "search differs in 8");
		docs.Should().Contain(d => d.Api!.OperationId == "bulk" && d.Api.Trees.Length == 2, "8 has no bulk");
		pages["8"]
			.Should()
			.Contain("/docs/api/doc/elasticsearch/v8/operation/operation-search-1", "GET search redirects to the shared page");
		docs.Should().Contain(d => d.Path == "/docs/api/doc/elasticsearch/operation/operation-search-1");
		var missing = docs
			.SelectMany(
				d => d.Api!.Trees.SelectMany(tree => TreeUrls(d).Select(url => (tree.Tree, Url: Rebase(url, d.Api.Trees[0], tree))))
			)
			.Where(x => !pages[x.Tree].Contains(x.Url))
			.Select(x => $"{x.Tree} {x.Url}")
			.ToList();
		missing.Should().BeEmpty();
	}

	/// <summary>The document's own URL and its breadcrumb URLs inside the API; the first crumb is the catalog, which is no tree's page.</summary>
	private static IEnumerable<string> TreeUrls(DocumentationDocument doc) => [doc.Path, .. doc.Parents.Skip(1).Select(static p => p.Path)];

	private static string Rebase(string url, ApiTreeEntry from, ApiTreeEntry to)
	{
		url.Should().StartWith(from.Root);
		return to.Root + url[from.Root.Length..];
	}

	private static HashSet<string> PageUrls(OpenApiGenerator generator, string moniker, OpenApiDocument spec)
	{
		var navigation = generator.CreateNavigation(ApiUrlBuilder.ProductSuffix("elasticsearch", moniker), spec);
		var root = navigation.Url.TrimEnd('/');
		return [.. OpenApiGenerator.PageNames(navigation).Keys.Select(name => root + name)];
	}

	/// <summary>The Elasticsearch fixture plus <c>GET /{index}/_search</c> as <c>search-1</c>, which shares the search page and leaves its own URL as a redirect.</summary>
	private static async Task<OpenApiDocument> Fixture()
	{
		var spec =
			(await OpenApiReader.Instance.ReadAsync(
				new FileSystem().FileInfo.New(Path.Join(AppContext.BaseDirectory, "TestData", "elasticsearch-openapi.json"))
			))!;
		var operations = spec.Paths["/{index}/_search"].Operations!;
		operations[HttpMethod.Get] = new OpenApiOperation(operations[HttpMethod.Post]) { OperationId = "search-1" };
		return spec;
	}
}
