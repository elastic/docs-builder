// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// End-to-end fixture modelled on the non-trivial <c>elastic/cloud</c> changelog configuration
/// (multi-product, a monthly profile, <c>bundle.repo</c> with repo != product, <c>link_allow_repos</c>,
/// <c>release_dates: false</c> and <c>rules.bundle.exclude_types</c>), but using
/// anonymized repo and product names only — never the real org/repo/product IDs.
///
/// It verifies the artifact-root (Option AD) behaviour: entries are sourced once from the authoring
/// pool (<c>changelog/{org}/{repo}/{branch}/...</c>), and the profile bundle that results is sound — resolved,
/// link-scrubbed against the allowlist, with the docs entry excluded and no auto-populated release date.
///
/// The authoring repo is an anonymized test name (<c>widget</c>) that deliberately differs from the
/// product IDs it publishes for — exactly the repo != product "moniker mismatch" that motivates Option
/// AD. Product IDs are drawn from the test harness's products.yml allowlist.
/// </summary>
public class CloudProfileFixtureTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private const string AuthoringRepo = "widget";

	// A feature entry whose only PR points at an allowlisted public repo (kept on scrub) plus a PR to a
	// non-allowlisted repo (scrubbed away).
	// language=yaml
	private const string FeatureEntry = """
		title: Faster hosted search
		type: feature
		products:
		  - product: cloud-hosted
		    target: 2026-05-15
		    lifecycle: ga
		prs:
		  - https://github.com/elastic/elasticsearch/pull/100
		  - https://github.com/elastic/widget-internal/pull/7
		""";

	// A docs entry that matches the product/target filter but must be dropped by rules.bundle.exclude_types.
	// language=yaml
	private const string DocsEntry = """
		title: Tidy up the hosted docs
		type: docs
		products:
		  - product: cloud-hosted
		    target: 2026-05-20
		    lifecycle: ga
		prs:
		  - https://github.com/elastic/elasticsearch/pull/200
		""";

	// A feature for a different product; excluded by the profile's cloud-hosted product filter.
	// language=yaml
	private const string OtherProductEntry = """
		title: Serverless-only change
		type: feature
		products:
		  - product: cloud-serverless
		    target: 2026-05-10
		    lifecycle: ga
		prs:
		  - https://github.com/elastic/kibana/pull/300
		""";

	// language=json
	private const string RegistryJson =
		"""{ "schema_version": 1, "product": "widget", "bundles": [ { "file": "1-feature.yaml" }, { "file": "2-docs.yaml" }, { "file": "3-other.yaml" } ] }""";

	private StubHandler RepoPoolHandler() => new(req =>
	{
		var path = req.RequestUri!.AbsolutePath;
		if (path.EndsWith("/registry.json", StringComparison.Ordinal))
			return Json(RegistryJson);
		if (path.EndsWith("1-feature.yaml", StringComparison.Ordinal))
			return Yaml(FeatureEntry);
		if (path.EndsWith("2-docs.yaml", StringComparison.Ordinal))
			return Yaml(DocsEntry);
		if (path.EndsWith("3-other.yaml", StringComparison.Ordinal))
			return Yaml(OtherProductEntry);
		return new HttpResponseMessage(HttpStatusCode.NotFound);
	});

	private CdnChangelogEntryFetcher Fetcher(StubHandler handler) =>
		new(new TestLoggerFactory(Output), handler, sleep: (_, _) => Task.CompletedTask);

	[Fact]
	public async Task MonthlyProfile_SourcesFromRepoPool_ProducesSoundBundle()
	{
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		// language=yaml
		var configContent =
			"""
			products:
			  available:
			    - cloud-hosted
			    - cloud-serverless
			pivot:
			  types:
			    feature: ">feature"
			    bug-fix: ">bug"
			    breaking-change: ">breaking"
			    docs: ">docs"
			rules:
			  bundle:
			    exclude_types: "docs"
			bundle:
			  output_directory: PLACEHOLDER
			  repo: widget
			  owner: elastic
			  release_dates: false
			  link_allow_repos:
			    - elastic/elasticsearch
			    - elastic/kibana
			  profiles:
			    wh-monthly:
			      products: "cloud-hosted {version}-* *"
			      output: "widget-{version}.yaml"
			      output_products: "cloud-hosted {version}"
			""".Replace("PLACEHOLDER", outputDir);

		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var handler = RepoPoolHandler();
		var service = new ChangelogBundlingService(LoggerFactory, ConfigurationContext, FileSystem, null, Fetcher(handler));

		var input = new BundleChangelogsArguments
		{
			Profile = "wh-monthly",
			ProfileArgument = "2026-05",
			Config = configPath
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		// Entries are sourced once from the authoring pool (changelog/{org}/{repo}/{branch}/...), not from
		// any product-scoped path. Owner comes from bundle.owner; branch defaults to "main".
		handler.RequestedPaths.Should().Contain($"/changelog/elastic/{AuthoringRepo}/main/registry.json");
		handler.RequestedPaths.Should().NotContain(p => p.Contains("/cloud-hosted/changelog/", StringComparison.Ordinal));

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().ContainSingle("the monthly profile writes a single bundle file");
		FileSystem.Path.GetFileName(outputFiles[0]).Should().Be("widget-2026-05.yaml");

		var bundle = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);

		// Sound bundle: the matching feature is present and resolved; the docs entry and the other-product
		// entry are excluded.
		bundle.Should().Contain("Faster hosted search");
		bundle.Should().NotContain("Tidy up the hosted docs", "rules.bundle.exclude_types drops docs entries");
		bundle.Should().NotContain("Serverless-only change", "the cloud-hosted product filter excludes other products");

		// Link allowlist: the allowlisted public PR is kept verbatim; the non-allowlisted repo reference is
		// rewritten to a "# PRIVATE:" sentinel rather than left as a live link.
		bundle.Should().Contain("- https://github.com/elastic/elasticsearch/pull/100");
		bundle.Should().Contain("# PRIVATE: https://github.com/elastic/widget-internal/pull/7",
			"non-allowlisted PR links must be scrubbed to a PRIVATE sentinel in bundle output");

		// release_dates: false → no auto-populated release date.
		bundle.Should().NotContain("release_date");
	}

	private static HttpResponseMessage Json(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	private static HttpResponseMessage Yaml(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/yaml") };

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public List<string> RequestedPaths { get; } = [];

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return responder(request);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
