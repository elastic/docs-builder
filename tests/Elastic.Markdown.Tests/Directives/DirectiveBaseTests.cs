// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using JetBrains.Annotations;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public abstract class DirectiveTest<TDirective>(ITestOutputHelper output, [LanguageInjection("markdown")]string content)
	: DirectiveTest(output, content)
	where TDirective : DirectiveBlock
{
	protected TDirective? Block { get; private set; }

	public override async Task InitializeAsync()
	{
		await base.InitializeAsync();
		Block = Document
			.Where(block => block is TDirective)
			.Cast<TDirective>()
			.FirstOrDefault();
	}

	[Fact]
	public void BlockIsNotNull() => Block.Should().NotBeNull();

}

public class TestDiagnosticsCollector(ILoggerFactory logger)
	: DiagnosticsCollector(logger, [])
{
	private readonly List<Diagnostic> _diagnostics = new();

	public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;

	protected override void HandleItem(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);
}

public abstract class DirectiveTest : IAsyncLifetime
{
	protected MarkdownFile File { get; }
	protected string Html { get; private set; }
	protected MarkdownDocument Document { get; private set; }
	protected MockFileSystem FileSystem { get; }
	protected TestDiagnosticsCollector Collector { get; }

	protected DirectiveTest(ITestOutputHelper output, [LanguageInjection("markdown")]string content)
	{
		var logger = new TestLoggerFactory(output);
		FileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			{ "docs/source/index.md", new MockFileData(content) }
		}, new MockFileSystemOptions
		{
			CurrentDirectory = Paths.Root.FullName
		});

		var file = FileSystem.FileInfo.New("docs/source/index.md");
		var root = file.Directory!;
		Collector = new TestDiagnosticsCollector(logger);
		var context = new BuildContext
		{
			ReadFileSystem = FileSystem,
			WriteFileSystem = FileSystem,
			Collector = Collector
		};
		var parser = new MarkdownParser(root, context);

		File = new MarkdownFile(file, root, parser, context);
		Html = default!; //assigned later
		Document = default!;
	}

	public virtual async Task InitializeAsync()
	{
		var collectTask =  Task.Run(async () => await Collector.StartAsync(default), default);

		Document = await File.ParseFullAsync(default);
		Html = File.CreateHtml(Document);
		Collector.Channel.TryComplete();

		await collectTask;
		await Collector.Channel.Reader.Completion;
		await Collector.StopAsync(default);
	}

	public Task DisposeAsync() => Task.CompletedTask;

}
