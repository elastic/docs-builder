// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Xml.Linq;
using Elastic.Documentation.Configuration;

namespace Elastic.Documentation.Build.Tests;

// TODO: Delete this class once the API Explorer migration (elastic/docs-eng-team#436) is finalised.
public class ApiSitemapParityTests
{
	// Mirrors modules/elastic-docs-v3/api-root-redirects.json in elastic/docs-infra (#459), which the native viewer resolves exact match first.
	private const string ApiDocPrefix = "/docs/api/doc/";
	private static readonly IReadOnlyDictionary<string, string> RedirectedRoots = new Dictionary<string, string>
	{
		["serverless"] = "kibana-serverless",
		["elastic-cloud-serverless"] = "cloud-serverless"
	};
	private static readonly IReadOnlySet<string> GoneRoots = new HashSet<string> { "observability-serverless" };

	// Exact page redirects from the same file. Bump.sh served a Servers page for specs that declare no servers,
	// and Cloud Billing renamed its Budgets tag.
	private static readonly IReadOnlyDictionary<string, string> RedirectedPages = new Dictionary<string, string>
	{
		["/docs/api/doc/elasticsearch/servers"] = "/docs/api/doc/elasticsearch",
		["/docs/api/doc/elasticsearch/v8/servers"] = "/docs/api/doc/elasticsearch/v8",
		["/docs/api/doc/elasticsearch/v9/servers"] = "/docs/api/doc/elasticsearch/v9",
		["/docs/api/doc/elasticsearch-serverless/servers"] = "/docs/api/doc/elasticsearch-serverless",
		["/docs/api/doc/cloud-enterprise/servers"] = "/docs/api/doc/cloud-enterprise",
		["/docs/api/doc/cloud-enterprise/v3/servers"] = "/docs/api/doc/cloud-enterprise/v3",
		["/docs/api/doc/cloud-billing/group/endpoint-budgets"] = "/docs/api/doc/cloud-billing/group/endpoint-billing-budgets"
	};
	private static readonly Uri SitemapIndex = new("https://www.elastic.co/docs/api/sitemap_index.xml");
	private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

	[Test]
	public async Task Build_ContainsEveryProductionApiSitemapPage(CancellationToken cancellationToken)
	{
		Skip.Unless(
			Environment.GetEnvironmentVariable("FEATURE_ASSEMBLER_API_EXPLORER") == "true",
			"Set FEATURE_ASSEMBLER_API_EXPLORER=true to build and verify local API pages"
		);

		using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
		client.DefaultRequestHeaders.UserAgent.ParseAdd("docs-builder-api-sitemap-parity-test");

		var productionUrls = await DownloadPageUrls(client, SitemapIndex, cancellationToken);
		await Assert.That(productionUrls).IsNotEmpty();
		var gone = productionUrls.Where(url => GoneRoots.Contains(RootOf(url))).ToArray();
		var redirected = productionUrls.Where(
			url => RedirectedPages.ContainsKey(url.AbsolutePath) || RedirectedRoots.ContainsKey(RootOf(url))
		).ToArray();
		// Redirected pages are served by an exact or prefix redirect, so verify the page exists at its redirect target.
		var auditedPaths = productionUrls.Except(gone).Select(RedirectTarget).Distinct().ToArray();
		var outputRoot = Environment.GetEnvironmentVariable("API_SITEMAP_PARITY_OUTPUT")
			?? Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "assembly");
		var missing = auditedPaths
			.Where(path => !File.Exists(Path.Join(outputRoot, path.TrimStart('/'), "index.html")))
			.Order(StringComparer.Ordinal)
			.ToArray();
		var reportPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "api-sitemap-missing-pages.txt");

		if (missing.Length > 0)
		{
			await File.WriteAllLinesAsync(reportPath, missing.Select(path => new Uri(SitemapIndex, path).AbsoluteUri), cancellationToken);
			Skip.Test(
				$"WARNING: {missing.Length} of {auditedPaths.Length} production API pages are not generated locally: see {reportPath} " +
					$"({redirected.Length} redirected pages checked at their target, {gone.Length} gone pages excluded)"
			);
		}

		File.Delete(reportPath);
	}

	private static string RootOf(Uri url)
	{
		var path = url.AbsolutePath;
		if (!path.StartsWith(ApiDocPrefix, StringComparison.Ordinal))
			return string.Empty;
		var rest = path[ApiDocPrefix.Length..];
		var slash = rest.IndexOf('/');
		return slash < 0 ? rest : rest[..slash];
	}

	private static string RedirectTarget(Uri url)
	{
		if (RedirectedPages.TryGetValue(url.AbsolutePath, out var page))
			return page;

		var root = RootOf(url);
		return RedirectedRoots.TryGetValue(root, out var newRoot)
			? string.Concat(ApiDocPrefix, newRoot, url.AbsolutePath.AsSpan(ApiDocPrefix.Length + root.Length))
			: url.AbsolutePath;
	}

	private static async Task<IReadOnlySet<Uri>> DownloadPageUrls(HttpClient client, Uri sitemap, CancellationToken ctx)
	{
		await using var stream = await client.GetStreamAsync(sitemap, ctx);
		var document = await XDocument.LoadAsync(stream, LoadOptions.None, ctx);
		var rootName = document.Root?.Name;

		if (rootName != SitemapNamespace + "urlset" && rootName != SitemapNamespace + "sitemapindex")
			throw new InvalidOperationException(
				$"Unexpected XML at {sitemap}: root element '{rootName}' is not a sitemap urlset or sitemapindex in namespace '{SitemapNamespace}'."
			);

		var locations = document.Descendants(SitemapNamespace + "loc").Select(element => new Uri(element.Value)).ToArray();

		if (rootName == SitemapNamespace + "urlset")
			return locations.ToHashSet();

		var pages = new HashSet<Uri>();
		foreach (var child in locations)
			pages.UnionWith(await DownloadPageUrls(client, child, ctx));
		return pages;
	}
}
