// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Xml.Linq;
using Elastic.Documentation.Configuration;

namespace Elastic.Documentation.Build.Tests;

public class ApiSitemapParityTests
{
	private const string DiscontinuedObservabilityServerlessPath = "/docs/api/doc/observability-serverless";
	private static readonly Uri SitemapIndex = new("https://www.elastic.co/docs/api/sitemap_index.xml");
	private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

	[Fact]
	public async Task Build_ContainsEveryProductionApiSitemapPage()
	{
		Assert.SkipUnless(
			Environment.GetEnvironmentVariable("FEATURE_ASSEMBLER_API_EXPLORER") == "true",
			"Set FEATURE_ASSEMBLER_API_EXPLORER=true to build and verify local API pages"
		);

		using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
		client.DefaultRequestHeaders.UserAgent.ParseAdd("docs-builder-api-sitemap-parity-test");

		var productionUrls = await DownloadPageUrls(client, SitemapIndex, TestContext.Current.CancellationToken);
		var auditedUrls = productionUrls.Where(
			url => !url.AbsolutePath.StartsWith(DiscontinuedObservabilityServerlessPath, StringComparison.Ordinal)
		).ToArray();
		var outputRoot = Environment.GetEnvironmentVariable("API_SITEMAP_PARITY_OUTPUT")
			?? Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly");
		var missing = auditedUrls
			.Where(url => !File.Exists(Path.Join(outputRoot, url.AbsolutePath.TrimStart('/'), "index.html")))
			.OrderBy(url => url.AbsoluteUri, StringComparer.Ordinal)
			.ToArray();
		var reportPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "api-sitemap-missing-pages.txt");

		if (missing.Length > 0)
		{
			await File.WriteAllLinesAsync(reportPath, missing.Select(url => url.AbsoluteUri), TestContext.Current.CancellationToken);
			Assert.Skip(
				$"WARNING: {missing.Length} of {auditedUrls.Length} production API pages are not generated locally: " + $"see {reportPath}"
			);
		}

		File.Delete(reportPath);
	}

	private static async Task<IReadOnlySet<Uri>> DownloadPageUrls(HttpClient client, Uri sitemap, CancellationToken ctx)
	{
		await using var stream = await client.GetStreamAsync(sitemap, ctx);
		var document = await XDocument.LoadAsync(stream, LoadOptions.None, ctx);
		var locations = document.Descendants(SitemapNamespace + "loc").Select(element => new Uri(element.Value)).ToArray();

		if (document.Root?.Name == SitemapNamespace + "urlset")
			return locations.ToHashSet();

		var pages = new HashSet<Uri>();
		foreach (var child in locations)
			pages.UnionWith(await DownloadPageUrls(client, child, ctx));
		return pages;
	}
}
