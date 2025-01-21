// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.IO;
using Elastic.Markdown.Tests.Directives;
using FluentAssertions;
using JetBrains.Annotations;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Inline;

public abstract class LeafTest<TDirective>(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: InlineTest(output, content)
	where TDirective : LeafInline
{
	protected TDirective? Block { get; private set; }

	public override async Task InitializeAsync()
	{
		await base.InitializeAsync();
		Block = Document
			.Descendants<TDirective>()
			.FirstOrDefault();
	}

	[Fact]
	public void BlockIsNotNull() => Block.Should().NotBeNull();

}

public abstract class BlockTest<TDirective>(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: InlineTest(output, content)
	where TDirective : Block
{
	protected TDirective? Block { get; private set; }

	public override async Task InitializeAsync()
	{
		await base.InitializeAsync();
		Block = Document
			.Descendants<TDirective>()
			.FirstOrDefault();
	}

	[Fact]
	public void BlockIsNotNull() => Block.Should().NotBeNull();

}

public abstract class InlineTest<TDirective>(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: InlineTest(output, content)
	where TDirective : ContainerInline
{
	protected TDirective? Block { get; private set; }

	public override async Task InitializeAsync()
	{
		await base.InitializeAsync();
		Block = Document
			.Descendants<TDirective>()
			.FirstOrDefault();
	}

	[Fact]
	public void BlockIsNotNull() => Block.Should().NotBeNull();

}
public abstract class InlineTest : IAsyncLifetime
{
	protected MarkdownFile File { get; }
	protected string Html { get; private set; }
	protected MarkdownDocument Document { get; private set; }
	protected TestDiagnosticsCollector Collector { get; }
	protected MockFileSystem FileSystem { get; }
	protected DocumentationSet Set { get; }

	private bool TestingFullDocument { get; }

	protected InlineTest(
		ITestOutputHelper output,
		[LanguageInjection("markdown")] string content,
		Dictionary<string, string>? globalVariables = null)
	{
		var logger = new TestLoggerFactory(output);
		TestingFullDocument = string.IsNullOrEmpty(content) || content.StartsWith("---");

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
		FileSystem.GenerateDocSetYaml(root, globalVariables);

		Collector = new TestDiagnosticsCollector(output);
		var context = new BuildContext(FileSystem)
		{
			Collector = Collector
		};
		Set = new DocumentationSet(context);
		File = Set.GetMarkdownFile(FileSystem.FileInfo.New("docs/index.md")) ?? throw new NullReferenceException();
		Html = default!; //assigned later
		Document = default!;
	}

	protected virtual void AddToFileSystem(MockFileSystem fileSystem) { }

	public virtual async Task InitializeAsync()
	{
		_ = Collector.StartAsync(default);

		await Set.ResolveDirectoryTree(default);

		Document = await File.ParseFullAsync(default);
		var html = File.CreateHtml(Document).AsSpan();
		var find = "</section>";
		var start = html.IndexOf(find, StringComparison.Ordinal);
		Html = start >= 0 && !TestingFullDocument
			? html[(start + find.Length)..].ToString().Trim(Environment.NewLine.ToCharArray())
			: html.ToString().Trim(Environment.NewLine.ToCharArray());
		Collector.Channel.TryComplete();
		await Collector.StopAsync(default);
	}

	public Task DisposeAsync() => Task.CompletedTask;

}
