// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class ApiMarkdownIntraApiLinkTests
{
	private sealed class CapturingRenderer : IMarkdownStringRenderer
	{
		public string? LastMarkdown { get; private set; }

		public string Render(string markdown, IFileInfo? source)
		{
			_ = source;
			LastMarkdown = markdown;
			return markdown;
		}
	}

	[Fact]
	public void Render_RewritesGroupAndOperationLinksAgainstCurrentApiBase()
	{
		var renderer = new CapturingRenderer();
		var collector = new DiagnosticsCollector([]);
		var fs = new FileSystem();
		var context = new BuildContext(
			collector,
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			TestHelpers.CreateConfigurationContext(fs)
		);
		var renderContext = new ApiRenderContext(
			context,
			new OpenApiDocument(),
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context))
		)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = new LandingNavigationItem("/api/doc/kibana").Index,
			MarkdownRenderer = renderer,
			ApiExplorerLog = null
		};

		var markdown =
			"""
			See [data views](../group/endpoint-data-views) and [export](../operation/operation-post-saved-objects-export).
			""";

		_ = ApiMarkdown.Render(renderContext, markdown);

		renderer.LastMarkdown.Should().Contain("(/api/doc/kibana/group/endpoint-data-views)");
		renderer.LastMarkdown.Should().Contain("(/api/doc/kibana/operation/operation-post-saved-objects-export)");
	}

	[Fact]
	public void CanonicalizeLinks_UsesLlmAbsoluteUrlStrategyWithoutMarkdownSuffix()
	{
		var markdown =
			"""
			[Regions](/docs/api/doc/cloud-serverless/group/endpoint-regions)
			[Relative](docs/api/doc/cloud-serverless/operation/operation-listregions)
			[External](https://example.com/reference)
			""";

		var rewritten = ApiMarkdown.CanonicalizeLinks(markdown, new Uri("https://www.elastic.co"));

		rewritten.Should().Contain("[Regions](https://www.elastic.co/docs/api/doc/cloud-serverless/group/endpoint-regions)");
		rewritten.Should().Contain("[Relative](https://www.elastic.co/docs/api/doc/cloud-serverless/operation/operation-listregions)");
		rewritten.Should().Contain("[External](https://example.com/reference)");
		rewritten.Should().NotContain("endpoint-regions.md");
	}
}
