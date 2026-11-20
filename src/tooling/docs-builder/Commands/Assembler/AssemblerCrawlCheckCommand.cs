// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.ApiCrawl;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Assembler;

internal sealed class AssemblerCrawlCheckCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration configuration
)
{
	/// <summary>Crawl every API Explorer URL on a deployed site and compare each answer with the API page-type table.</summary>
	/// <remarks>
	/// <para>
	/// The API URLs come from the floating sections of a published <c>links.json</c>, plus every URL in
	/// <c>sitemap-api.xml</c>. Each page must answer 200 without a redirect, name the canonical URL and robots value its page
	/// type gives it, and be in the sitemap only when its type belongs there. The sitemap index must list
	/// <c>sitemap-api.xml</c>. A sample of old <c>/docs/api/doc/</c> URLs must redirect once to their current page.
	/// </para>
	/// <para>
	/// Each difference is an error and a summary per page type is printed. The command exits 1 on any difference.
	/// Staging is behind Google sign-in, so crawl edge or prod.
	/// </para>
	/// </remarks>
	/// <param name="environment">The environment in <c>config/assembler.yml</c> to crawl. Its <c>uri</c> and <c>path_prefix</c> give the site and its <c>allow_indexing</c> the expected robots value.</param>
	/// <param name="baseUrl">Crawl this scheme and host instead of the environment's <c>uri</c>, for example <c>http://localhost:4000</c>.</param>
	/// <param name="linksJson">The path or URL of the <c>links.json</c> that lists the API pages. Defaults to docs-content <c>main</c> in the link index.</param>
	/// <param name="redirectStatus">The status an old <c>/docs/api/doc/</c> URL must redirect with.</param>
	/// <param name="redirectSample">How many API pages to check through their old <c>/docs/api/doc/</c> URL, on top of the redirect table and every tree root.</param>
	/// <param name="concurrency">How many requests to run at once.</param>
	[CommandName("crawl-check")]
	public async Task<int> CrawlCheck(
		GlobalCliOptions _,
		string environment = "edge",
		[Url] string? baseUrl = null,
		string? linksJson = null,
		[Range(300, 399)] int redirectStatus = 302,
		[Range(0, 10_000)] int redirectSample = 50,
		[Range(1, 256)] int concurrency = 16,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		if (!configuration.Environments.TryGetValue(environment, out var publish))
		{
			collector.EmitGlobalError(
				$"Environment '{environment}' is not in config/assembler.yml. Known: {string.Join(", ", configuration.Environments.Keys)}"
			);
			return await serviceInvoker.InvokeAsync(ct);
		}

		var options = new ApiCrawlOptions(
			new Uri(baseUrl ?? publish.Uri),
			publish.PathPrefix ?? string.Empty,
			linksJson ?? ApiCrawlOptions.DefaultLinksJson,
			publish.AllowIndexing
		)
		{ RedirectStatus = redirectStatus, RedirectSample = redirectSample, Concurrency = concurrency };
		var service = new ApiCrawlService(logFactory, new FileSystem(), Console.Out);
		serviceInvoker.AddCommand(service, options, static async (s, col, o, ctx) => await s.CrawlAsync(col, o, ctx));
		return await serviceInvoker.InvokeAsync(ct);
	}
}
