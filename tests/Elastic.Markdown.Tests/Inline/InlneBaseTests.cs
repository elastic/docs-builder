// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;
using FluentAssertions;
using JetBrains.Annotations;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Inline;

public abstract class LeafTest<TDirective>(ITestOutputHelper output, [LanguageInjection("markdown")]string content)
	: InlineTest(output, content)
	where TDirective : LeafInline
{
	protected TDirective? Block { get; private set; }

	public override async Task InitializeAsync()
	{
		await base.InitializeAsync();
		Block = Document
			.Where(block => block is ParagraphBlock)
			.Cast<ParagraphBlock>()
			.FirstOrDefault()?
			.Inline?
			.Where(block => block is TDirective)
			.Cast<TDirective>()
			.FirstOrDefault();
	}

	[Fact]
	public void BlockIsNotNull() => Block.Should().NotBeNull();

}

public abstract class InlineTest<TDirective>(ITestOutputHelper output, [LanguageInjection("markdown")]string content)
	: InlineTest(output, content)
	where TDirective : ContainerInline
{
	protected TDirective? Block { get; private set; }

	public override async Task InitializeAsync()
	{
		await base.InitializeAsync();
		Block = Document
			.Where(block => block is ParagraphBlock)
			.Cast<ParagraphBlock>()
			.FirstOrDefault()?
			.Inline?
			.Where(block => block is TDirective)
			.Cast<TDirective>()
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

	protected InlineTest(ITestOutputHelper output, [LanguageInjection("markdown")]string content)
	{
		var logger = new TestLoggerFactory(output);
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/source/index.md", new MockFileData(content) }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.Root.FullName
		});

		var file = fileSystem.FileInfo.New("docs/source/index.md");
		var root = fileSystem.DirectoryInfo.New(Paths.Root.FullName);
		var context = new BuildContext
		{
			ReadFileSystem = fileSystem,
			WriteFileSystem = fileSystem,
			Collector = new DiagnosticsCollector(logger, [])
		};
		var parser = new MarkdownParser(root, context);

		File = new MarkdownFile(file, root, parser, context);
		Html = default!; //assigned later
		Document = default!;
	}

	public virtual async Task InitializeAsync()
	{
		Document = await File.ParseFullAsync(default);
		Html = File.CreateHtml(Document);
	}

	public Task DisposeAsync() => Task.CompletedTask;

}
