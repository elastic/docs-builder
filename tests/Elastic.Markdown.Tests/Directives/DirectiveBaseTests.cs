// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using JetBrains.Annotations;
using Markdig.Syntax;

namespace Elastic.Markdown.Tests.Directives;

public abstract class DirectiveTest<TDirective>(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: DirectiveTest(output, content)
	where TDirective : DirectiveBlock
{
	protected TDirective? Block { get; private set; }

	public override async ValueTask InitializeAsync()
	{
		await base.InitializeAsync();
		Block = Document
			.Descendants<TDirective>()
			.FirstOrDefault();
	}

	[Fact]
	public void BlockIsNotNull() => Block.Should().NotBeNull();
}

public abstract class DirectiveTest : IAsyncLifetime
{
	protected MarkdownFile File { get; }
	protected string Html { get; private set; }
	protected MarkdownDocument Document { get; private set; }
	protected MockFileSystem FileSystem { get; }
	protected TestDiagnosticsCollector Collector { get; }
	protected DocumentationSet Set { get; set; }

	private bool TestingFullDocument { get; }

	protected DirectiveTest(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	{
		var logger = new TestLoggerFactory(output);

		TestingFullDocument = string.IsNullOrEmpty(content) || content.StartsWith("---", StringComparison.OrdinalIgnoreCase);
		var documentContents = TestingFullDocument ? content :
// language=markdown
$"""
 # Test Document

 {content}
 """;

		FileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/index.md", new MockFileData(documentContents) }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.Root.FullName
		});
		// ReSharper disable once VirtualMemberCallInConstructor
		// nasty but sub implementations won't use class state.
		AddToFileSystem(FileSystem);

		var root = FileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName, "docs/"));
		FileSystem.GenerateDocSetYaml(root);

		Collector = new TestDiagnosticsCollector(output);
		var context = new BuildContext(FileSystem)
		{
			Collector = Collector
		};
		var linkResolver = new TestCrossLinkResolver();
		Set = new DocumentationSet(context, logger, linkResolver);
		File = Set.GetMarkdownFile(FileSystem.FileInfo.New("docs/index.md")) ?? throw new NullReferenceException();
		Html = default!; //assigned later
		Document = default!;
	}

	protected virtual void AddToFileSystem(MockFileSystem fileSystem) { }

	public virtual async ValueTask InitializeAsync()
	{
		_ = Collector.StartAsync(default);

		await Set.LinkResolver.FetchLinks();
		Document = await File.ParseFullAsync(default);
		var html = MarkdownFile.CreateHtml(Document).AsSpan();
		var find = "</section>";
		var start = html.IndexOf(find, StringComparison.Ordinal);
		Html = start >= 0
			? html[(start + find.Length)..].ToString().Trim(Environment.NewLine.ToCharArray())
			: html.ToString().Trim(Environment.NewLine.ToCharArray());
		Collector.Channel.TryComplete();

		await Collector.StopAsync(default);
	}

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
