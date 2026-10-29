// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using TUnit.Core.Interfaces;

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

	/// <summary>Reads ATX headings line by line, enough for tests that check which heading ids a page keeps.</summary>
	public IReadOnlyList<TocItem> ReadApiDescriptionHeadings(string markdown, IFileInfo? source) =>
		[
			.. markdown
				.ReplaceLineEndings("\n")
				.Split('\n')
				.Select(line => (Level: line.TakeWhile(c => c == '#').Count(), Line: line))
				.Where(h => h.Level is >= 2 and <= 6 && h.Line.Length > h.Level && h.Line[h.Level] == ' ')
				.Select(h => new TocItem(h.Line[h.Level..].Trim(), h.Line[h.Level..].Trim().Slugify(), h.Level))
		];
}

/// <summary>
/// Loads the fixture spec and builds its navigation tree once for all tests sharing the fixture.
/// </summary>
public class ApiExplorerFixture : IAsyncInitializer, IAsyncDisposable
{
	protected virtual string SpecFileName => "api-explorer-fixture.json";

	public BuildContext Context { get; private set; } = null!;
	public OpenApiDocument Document { get; private set; } = null!;
	public LandingNavigationItem Navigation { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		var realFs = new FileSystem();
		var configurationContext = TestHelpers.CreateConfigurationContext(realFs);
		var invocation = realFs.DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName);
		Context = new BuildContext(new DiagnosticsCollector([]), DocumentationFileSystem.Resolve(invocation), configurationContext);

		var fs = new FileSystem();
		var path = fs.Path.Combine(AppContext.BaseDirectory, "TestData", SpecFileName);
		Document = await OpenApiReader.Instance.ReadAsync(fs.FileInfo.New(path))
			?? throw new InvalidOperationException($"Could not read fixture spec at {path}");

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, Context, PassthroughMarkdownRenderer.Instance);
		Navigation = generator.CreateNavigation("fixture", Document);
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;

	public IEnumerable<INavigationItem> Walk() => Walk(Navigation);

	public static IEnumerable<INavigationItem> Walk(INavigationItem item)
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

/// <summary>A small spec with document-level <c>security</c>, and operations that inherit it, repeat it, narrow it, make it optional and drop it.</summary>
public sealed class DocumentSecurityFixture : ApiExplorerFixture
{
	protected override string SpecFileName => "api-explorer-document-security.json";
}
