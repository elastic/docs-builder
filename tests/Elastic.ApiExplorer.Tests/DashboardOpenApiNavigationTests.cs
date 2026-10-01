// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.ApiExplorer;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.ApiExplorer.Tests;

public partial class DashboardOpenApiNavigationTests
{
	/// <summary>
	/// Dashboard OpenAPI (single tag for all operations) must populate the API explorer nav.
	/// Regression: grouped navigation overwrote root items with an empty top-level list.
	/// </summary>
	[Test]
	public async Task CreateNavigation_SingleTagOpenApiSpec_HasSidebarItems()
	{
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			configurationContext
		);
		var fs = new FileSystem();
		var path = fs.Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "dashboard-openapi.json");
		var fi = fs.FileInfo.New(path);
		var doc = await OpenApiReader.Instance.ReadAsync(fi);

		doc.Should().NotBeNull();

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("dashboard", doc);

		navigation.NavigationItems.Should().NotBeEmpty();
	}

	[Test]
	public async Task GroupPage_OperationRow_IsOneLinkWithCodePath()
	{
		var html = await RenderDashboardGroupPage();
		var paths = ApiUrl().Matches(html);

		paths.Should().NotBeEmpty();
		foreach (Match path in paths)
			OpenAnchor(html, path.Index).Should().Contain("api-overview-title");

		var css = await File.ReadAllTextAsync(
			Path.Combine(Paths.WorkingDirectoryRoot.FullName, "src", "Elastic.Documentation.Site", "Assets", "api-docs.css"),
			TestContext.Current!.Execution.CancellationToken
		);
		css.Should().MatchRegex("""(?s)\.api-page-intro \.api-url,\s*\.api-overview \.api-url\s*\{[^}]*'Roboto Mono'""");
		css.Should().MatchRegex(
			"""(?s)a\.api-overview-title:hover,\s*a\.api-overview-title:focus-visible\s*\{[^}]*background-color:\s*#ecf1f9"""
		);
		css.Should().MatchRegex("""(?s)a\.api-overview-title:focus-visible\s*\{[^}]*outline:\s*2px solid""");
	}

	private static string OpenAnchor(string html, int index)
	{
		var before = html[..index];
		var open = before.LastIndexOf("<a ", StringComparison.Ordinal);
		var close = before.LastIndexOf("</a>", StringComparison.Ordinal);
		open.Should().BeGreaterThan(close);
		return before[open..];
	}

	private static async Task<string> RenderDashboardGroupPage()
	{
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			configurationContext
		);
		var fs = new FileSystem();
		var path = fs.Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "dashboard-openapi.json");
		var doc = await OpenApiReader.Instance.ReadAsync(fs.FileInfo.New(path));
		doc.Should().NotBeNull();

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("dashboard", doc);
		var tag = Walk(navigation).OfType<TagNavigationItem>().First();
		var renderContext = new ApiRenderContext(
			context,
			doc,
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context))
		)
		{ NavigationHtml = string.Empty, CurrentNavigation = tag, MarkdownRenderer = PassthroughMarkdownRenderer.Instance };
		var output = new MockFileSystem();
		await using (var stream = output.FileStream.New("/out.html", FileMode.Create, FileAccess.Write))
			await tag.Index.Model.RenderAsync(stream, renderContext, TestContext.Current!.Execution.CancellationToken);

		return output.File.ReadAllText("/out.html");
	}

	private static IEnumerable<INavigationItem> Walk(INavigationItem item)
	{
		yield return item;
		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			yield break;

		foreach (var child in node.NavigationItems)
		{
			foreach (var descendant in Walk(child))
				yield return descendant;
		}
	}

	[GeneratedRegex("""<span class="api-url">([^<]+)</span>""")]
	private static partial Regex ApiUrl();
}
