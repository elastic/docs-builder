// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
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
	protected MarkdownFile File { get; }
	protected string Html { get; private set; }
	protected MarkdownDocument Document { get; private set; }
	protected TestDiagnosticsCollector Collector { get; }
	protected MockFileSystem FileSystem { get; }
	protected DocumentationSet Set { get; }

	private bool TestingFullDocument { get; }

	protected InlineTest([LanguageInjection("markdown")] string content, Dictionary<string, string>? globalVariables = null)
	{
		var logger = new TestLoggerFactory();
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
		FileSystem.GenerateDocSetYaml(root, globalVariables);

		Collector = new TestDiagnosticsCollector();
		var configurationContext = TestHelpers.CreateConfigurationContext(FileSystem);
		var context = CreateBuildContext(Collector, FileSystem, configurationContext);
		var linkResolver = CreateCrossLinkResolver();
		Set = new DocumentationSet(context, logger, linkResolver);
		File = Set.TryFindDocument(FileSystem.FileInfo.New("docs/index.md")) as MarkdownFile ?? throw new NullReferenceException();
		Html = default!; //assigned later
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
		_ = Collector.StartAsync(TestContext.Current!.Execution.CancellationToken);

		await Set.ResolveDirectoryTree(TestContext.Current!.Execution.CancellationToken);

		Document = await File.ParseFullAsync(Set.TryFindDocumentByRelativePath, TestContext.Current!.Execution.CancellationToken);
		var html = MarkdownFile.CreateHtml(Document).AsSpan();
		var find = "</h1>\n</section>";
		var start = html.IndexOf(find, StringComparison.Ordinal);
		Html = start >= 0 && !TestingFullDocument
			? html[(start + find.Length)..].ToString().Trim(Environment.NewLine.ToCharArray())
			: html.ToString().Trim(Environment.NewLine.ToCharArray());
		await Collector.StopAsync(TestContext.Current!.Execution.CancellationToken);
	}

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
