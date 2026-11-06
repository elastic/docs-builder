// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.Collections.Concurrent;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Markdown.IO;
using JetBrains.Annotations;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using TUnit.Core.Interfaces;

namespace Elastic.Markdown.Tests.Inline;

public abstract class LeafTest<TDirective>([LanguageInjection("markdown")] string content) : InlineTest(
	content
) where TDirective : LeafInline
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

public abstract class BlockTest<TDirective>([LanguageInjection("markdown")] string content) : InlineTest(
	content,
	new Dictionary<string, string> { { "a-variable", "This is a variable" } }
) where TDirective : Block
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

public abstract class InlineTest<TDirective>(
	[LanguageInjection("markdown")] string content,
	Dictionary<string, string>? globalVariables = null
) : InlineTest(content, globalVariables) where TDirective : ContainerInline
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

public abstract class InlineTest : IAsyncInitializer, IAsyncDisposable
{
	// TUnit creates a new instance for every test method, but building the documentation set, parsing and rendering the document
	// is the expensive part. It is built once per concrete test class and shared by that class's tests, which only read from it.
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
	protected TestDiagnosticsCollector Collector { get; private set; }
	protected MockFileSystem FileSystem { get; private set; }
	protected DocumentationSet Set { get; private set; }

	private bool TestingFullDocument { get; }

	protected InlineTest([LanguageInjection("markdown")] string content, Dictionary<string, string>? globalVariables = null)
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
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName, }
		);
		// ReSharper disable once VirtualMemberCallInConstructor
		// nasty but sub implementations won't use class state.
		AddToFileSystem(FileSystem);
		var baseRootPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? Paths.WorkingDirectoryRoot.FullName.Replace('\\', '/')
			: Paths.WorkingDirectoryRoot.FullName;
		var root = FileSystem.DirectoryInfo.New($"{baseRootPath}/docs/");
		// The docset lists the Markdown files present now, so it has to be generated before a subclass constructor adds more.
		FileSystem.GenerateDocSetYaml(root, globalVariables);

		// Assigned from the shared fixture in InitializeAsync.
		File = null!;
		Set = null!;
		Collector = null!;
		Html = default!;
		Document = default!;
	}

	protected virtual void AddToFileSystem(MockFileSystem fileSystem) { }

	/// <summary>Override to provide a different cross-link resolver (e.g. codex-aware).</summary>
	protected virtual ICrossLinkResolver CreateCrossLinkResolver() => new TestCrossLinkResolver();

	/// <summary>Override to customize BuildContext (e.g. for codex tests).</summary>
	protected virtual BuildContext CreateBuildContext(
		TestDiagnosticsCollector collector,
		MockFileSystem fileSystem,
		IConfigurationContext configurationContext
	) => new(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext) { UrlPathPrefix = "/docs" };

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
		var collector = new TestDiagnosticsCollector();
		var configurationContext = TestHelpers.CreateConfigurationContext(FileSystem);
		var context = CreateBuildContext(collector, FileSystem, configurationContext);
		var linkResolver = CreateCrossLinkResolver();
		var set = new DocumentationSet(context, new TestLoggerFactory(), linkResolver);
		var file = set.TryFindDocument(FileSystem.FileInfo.New("docs/index.md")) as MarkdownFile ?? throw new NullReferenceException();

		_ = collector.StartAsync(ctx);

		await set.ResolveDirectoryTree(ctx);

		var document = await file.ParseFullAsync(set.TryFindDocumentByRelativePath, ctx);
		var html = MarkdownFile.CreateHtml(document).AsSpan();
		var find = "</h1>\n</section>";
		var start = html.IndexOf(find, StringComparison.Ordinal);
		var renderedHtml = start >= 0 && !TestingFullDocument
			? html[(start + find.Length)..].ToString().Trim(Environment.NewLine.ToCharArray())
			: html.ToString().Trim(Environment.NewLine.ToCharArray());
		await collector.StopAsync(ctx);

		return new BuiltFixture(file, renderedHtml, document, collector, FileSystem, set);
	}

	// The fixture is shared by every test of the class and lives for the whole run, so there is nothing to release per test.
	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
