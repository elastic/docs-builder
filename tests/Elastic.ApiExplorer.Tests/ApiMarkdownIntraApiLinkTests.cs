// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;
using Nullean.ScopedFileSystem;

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
		var context = new BuildContext(collector, FileSystemFactory.RealGitRootForPath(null), TestHelpers.CreateConfigurationContext(fs));
		var renderContext = new ApiRenderContext(context, new OpenApiDocument(), new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = new StubNavigationItem("/api/kibana/operation/operation-foo"),
			MarkdownRenderer = renderer,
			ApiExplorerLog = null
		};

		var markdown = """
			See [data views](../group/endpoint-data-views) and [export](../operation/operation-post-saved-objects-export).
			""";

		_ = ApiMarkdown.Render(renderContext, markdown);

		renderer.LastMarkdown.Should().Contain("(/api/kibana/group/endpoint-data-views)");
		renderer.LastMarkdown.Should().Contain("(/api/kibana/operation/operation-post-saved-objects-export)");
	}

	private sealed class StubNavigationItem(string url) : INavigationItem
	{
		public string Url { get; } = url;
		public string NavigationTitle => "stub";
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => throw new NotSupportedException();
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
	}
}
