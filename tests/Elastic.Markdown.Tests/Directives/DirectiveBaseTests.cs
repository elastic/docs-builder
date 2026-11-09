// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.Collections.Concurrent;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Directives;
using JetBrains.Annotations;
using Markdig.Syntax;
using TUnit.Core.Interfaces;

namespace Elastic.Markdown.Tests.Directives;

public abstract class DirectiveTest<TDirective>([LanguageInjection("markdown")] string content) : DirectiveTest(
	content
) where TDirective : DirectiveBlock
{
	protected TDirective? Block { get; private set; }

	public override async Task InitializeAsync()
	{
		await base.InitializeAsync();
		Block = Document.Descendants<TDirective>().FirstOrDefault();
	}

	[Test]
	public void BlockIsNotNull() => Block.Should().NotBeNull();
}

public abstract class DirectiveTest : IAsyncInitializer, IAsyncDisposable
{
	// TUnit creates a new instance for every test method, but building the documentation set, parsing and rendering the document
	// is the expensive part. It is built once per concrete test class and shared by that class's tests, which only read from it.
	// Anything a class needs to change on the shared objects goes through ConfigureDocumentationSet or OnFixtureBuilt, which run once.
	private static readonly ConcurrentDictionary<Type, Lazy<Task<BuiltFixture>>> Fixtures = new();

	private sealed record BuiltFixture(
		MarkdownFile File,
		string Html,
		MarkdownDocument Document,
		TestDiagnosticsCollector Collector,
		MockFileSystem FileSystem,
		DocumentationSet Set
	);

	protected MarkdownFile File { get; private set; }
	protected string Html { get; private set; }
	protected MarkdownDocument Document { get; private set; }
	protected MockFileSystem FileSystem { get; private set; }
	protected TestDiagnosticsCollector Collector { get; private set; }
	protected DocumentationSet Set { get; private set; }

	private bool TestingFullDocument { get; }

	protected DirectiveTest([LanguageInjection("markdown")] string content)
	{
		TestingFullDocument = string.IsNullOrEmpty(content) || content.StartsWith("---", StringComparison.OrdinalIgnoreCase);
		var documentContents = TestingFullDocument
			? content
			:
			// language=markdown
			$"""
 # Test Document

 {content}
 """;

		FileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData> { { "docs/index.md", new MockFileData(documentContents) } },
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		// ReSharper disable once VirtualMemberCallInConstructor
		// nasty but sub implementations won't use class state.
		AddToFileSystem(FileSystem);

		var root = FileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/"));
		// The docset lists the Markdown files present now, so it has to be generated before a subclass constructor adds more.
		// ReSharper disable once VirtualMemberCallInConstructor
		FileSystem.GenerateDocSetYaml(root, products: GetDocsetProducts(), extraYaml: GetDocsetExtraYaml());

		// Assigned from the shared fixture in InitializeAsync.
		File = null!;
		Set = null!;
		Collector = null!;
		Html = default!;
		Document = default!;
	}

	protected virtual void AddToFileSystem(MockFileSystem fileSystem) { }

	/// <summary>
	/// Override to supply an explicit <see cref="GitCheckoutInformation"/> for the build context.
	/// Returns <see langword="null"/> by default (factory produces canned test data).
	/// </summary>
	protected virtual GitCheckoutInformation? GetGitCheckoutInformation() => null;

	/// <summary>
	/// Override to specify products for the docset configuration.
	/// Returns null by default (no products configured).
	/// </summary>
	protected virtual IReadOnlyList<string>? GetDocsetProducts() => null;

	protected virtual string? GetDocsetExtraYaml() => null;

	/// <summary>
	/// Override to inject a resolver of CDN-prefetched changelog bundles for the <c>{changelog}</c>
	/// <c>:cdn:</c> directive. Returns null by default (no-op resolver), so non-CDN tests are unaffected.
	/// </summary>
	protected virtual IReleaseNotesResolver? GetReleaseNotesResolver() => null;

	/// <summary>Override to inject a deterministic environment for env-dependent config (e.g. <c>storybook.registry</c>).</summary>
	protected virtual IEnvironmentVariables? GetEnvironment() => null;

	/// <summary>
	/// Override to simulate an assembler build publishing a specific content source
	/// (<c>current</c> = production, <c>next</c> = staging). Null (default) mimics isolated builds.
	/// </summary>
	protected virtual ContentSource? GetContentSource() => null;

	/// <summary>
	/// Runs once per test class, right after the documentation set is created and before its directory tree is resolved.
	/// Use it to configure the set. Do not touch the set from a test or from <see cref="InitializeAsync"/>: it is shared.
	/// </summary>
	protected virtual void ConfigureDocumentationSet(DocumentationSet set) { }

	/// <summary>
	/// Runs once per test class, after the document is parsed and rendered.
	/// Use it to change the parsed document, for example a directive block. Do not do that from a test or from
	/// <see cref="InitializeAsync"/>: the document is shared by the tests of the class, which run in parallel.
	/// </summary>
	protected virtual void OnFixtureBuilt(MarkdownDocument document) { }

	public virtual async Task InitializeAsync()
	{
		var fixture = await Fixtures.GetOrAdd(GetType(), _ => new Lazy<Task<BuiltFixture>>(BuildAsync)).Value;
		File = fixture.File;
		Html = fixture.Html;
		Document = fixture.Document;
		Collector = fixture.Collector;
		FileSystem = fixture.FileSystem;
		Set = fixture.Set;
	}

	// Runs on the first instance of the class only, after its constructors (including the subclass constructor) have finished.
	private async Task<BuiltFixture> BuildAsync()
	{
		var ctx = Cancel.None;
		var root = FileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs/"));
		var collector = new TestDiagnosticsCollector();
		var configurationContext = TestHelpers.CreateConfigurationContext(FileSystem);
		var context = new BuildContext(
			collector,
			TestHelpers.CreateDocumentationFileSystem(FileSystem, root, GetGitCheckoutInformation()),
			configurationContext,
			GetEnvironment()
		)
		{ ContentSource = GetContentSource() };
		var set = new DocumentationSet(context, new TestLoggerFactory(), new TestCrossLinkResolver(), GetReleaseNotesResolver());
		var file = set.TryFindDocument(FileSystem.FileInfo.New("docs/index.md")) as MarkdownFile ?? throw new NullReferenceException();
		ConfigureDocumentationSet(set);

		_ = collector.StartAsync(ctx);

		await set.ResolveDirectoryTree(ctx);
		var document = await file.ParseFullAsync(set.TryFindDocumentByRelativePath, ctx);
		var html = MarkdownFile.CreateHtml(document).AsSpan();
		var find = "</section>";
		var start = html.IndexOf(find, StringComparison.Ordinal);
		var renderedHtml = start >= 0
			? html[(start + find.Length)..].ToString().Trim(Environment.NewLine.ToCharArray())
			: html.ToString().Trim(Environment.NewLine.ToCharArray());

		await collector.StopAsync(ctx);
		OnFixtureBuilt(document);

		return new BuiltFixture(file, renderedHtml, document, collector, FileSystem, set);
	}

	/// <summary>
	/// Returns the content of all SVG files written to the output directory during rendering.
	/// Use this to assert on diagram content after the switch from inline SVG to external files.
	/// </summary>
	protected IReadOnlyList<string> ReadMermaidSvgs()
	{
		var outputDir = Set.Context.OutputDirectory.FullName;
		return FileSystem
			.AllFiles
			.Where(
				f => f.StartsWith(outputDir, StringComparison.OrdinalIgnoreCase) && f.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
			)
			.Select(f => FileSystem.File.ReadAllText(f))
			.ToList();
	}

	// The fixture is shared by every test of the class and lives for the whole run, so there is nothing to release per test.
	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
