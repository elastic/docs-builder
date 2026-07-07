// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Nullean.ScopedFileSystem;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// Renders markdown as a deterministic <c>&lt;p&gt;</c> wrapper so tests cover description plumbing
/// without depending on the full markdown pipeline. <see cref="NoopMarkdownStringRenderer"/> would blank
/// all descriptions and hide regressions.
/// </summary>
public sealed class PassthroughMarkdownRenderer : IMarkdownStringRenderer
{
	private PassthroughMarkdownRenderer() { }

	public static PassthroughMarkdownRenderer Instance { get; } = new();

	public string Render(string markdown, IFileInfo? source) => $"<p>{markdown}</p>";
}

/// <summary>
/// Loads the fixture spec and builds its navigation tree once for all tests sharing the fixture.
/// </summary>
public sealed class ApiExplorerFixture : IAsyncLifetime
{
	public BuildContext Context { get; private set; } = null!;
	public OpenApiDocument Document { get; private set; } = null!;
	public LandingNavigationItem Navigation { get; private set; } = null!;

	public async ValueTask InitializeAsync()
	{
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		// RealGitRootForPath(null) rather than RealRead: it adds the main repo's .git dir as a scope
		// root when the checkout is a git worktree, which BuildContext needs to read git information.
		Context = new BuildContext(new DiagnosticsCollector([]), FileSystemFactory.RealGitRootForPath(null), configurationContext);

		var fs = new FileSystem();
		var path = fs.Path.Combine(AppContext.BaseDirectory, "TestData", "api-explorer-fixture.json");
		Document = await OpenApiReader.Create(fs.FileInfo.New(path))
			?? throw new InvalidOperationException($"Could not read fixture spec at {path}");

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, Context, PassthroughMarkdownRenderer.Instance);
		Navigation = generator.CreateNavigation("fixture", Document);
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;

	public IEnumerable<INavigationItem> Walk() => Walk(Navigation);

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
}
