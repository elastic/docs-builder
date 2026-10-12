// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// Spec descriptions use bump.sh blockquotes and "NOTE:"-style labels for callouts.
/// See https://github.com/elastic/docs-eng-team/issues/1011
/// </summary>
public class ApiAdmonitionsTests
{
	[Test]
	[Arguments("info", "note")]
	[Arguments("warn", "warning")]
	public void Convert_BlockquoteTypeLine_BecomesDirective(string type, string directive)
	{
		var result = ApiAdmonitions.Convert($"Intro.\n\n> {type}\n> First line.\n> Second line.\n\nAfter.");

		result.Should().Be($"Intro.\n\n:::{{{directive}}}\nFirst line.\nSecond line.\n:::\n\nAfter.");
	}

	[Test]
	public void Convert_BlockquoteDirectlyAfterParagraph_IsSeparatedByBlankLine()
	{
		var result = ApiAdmonitions.Convert("Some text.\n> info\n> You cannot do that.\n\nMore.");

		result.Should().Be("Some text.\n\n:::{note}\nYou cannot do that.\n:::\n\nMore.");
	}

	[Test]
	public void Convert_PlainBlockquote_IsKept()
	{
		var markdown = "> The quick brown fox.";

		ApiAdmonitions.Convert(markdown).Should().Be(markdown);
	}

	[Test]
	[Arguments("NOTE: Body.", "note")]
	[Arguments("Note: Body.", "note")]
	[Arguments("**Note:** Body.", "note")]
	[Arguments("IMPORTANT: Body.", "important")]
	[Arguments("**IMPORTANT**: Body.", "important")]
	[Arguments("WARNING: Body.", "warning")]
	[Arguments("**WARNING:** Body.", "warning")]
	[Arguments("TIP: Body.", "tip")]
	[Arguments("DEPRECATED: Body.", "warning")]
	public void Convert_LabelParagraph_BecomesDirective(string paragraph, string directive)
	{
		var result = ApiAdmonitions.Convert($"Intro.\n\n{paragraph}\nContinues here.\n\nAfter.");

		result.Should().Be($"Intro.\n\n:::{{{directive}}}\nBody.\nContinues here.\n:::\n\nAfter.");
	}

	[Test]
	public void Convert_LabelOnlyAtParagraphStart()
	{
		var markdown = "Skill IDs to enable. Note: only restricts when set.\n\nSee also:\nNOTE: not a paragraph start.";

		ApiAdmonitions.Convert(markdown).Should().Be(markdown);
	}

	[Test]
	public void Convert_LeavesFencedCodeAlone()
	{
		var markdown = "```text\nNOTE: keep me\n> info\n> keep me\n```";

		ApiAdmonitions.Convert(markdown).Should().Be(markdown);
	}

	[Test]
	public void Convert_ShorterFenceInsideLongerFence_DoesNotCloseIt()
	{
		var markdown = "````md\n```\nNOTE: keep me\n```\n> info\n> keep me\n````";

		ApiAdmonitions.Convert(markdown).Should().Be(markdown);
	}

	[Test]
	public void Convert_FencedCodeInsideBlockquote_IsKept()
	{
		var markdown = "> ```text\n> info\n> keep me\n> ```\n\nAfter.";

		ApiAdmonitions.Convert(markdown).Should().Be(markdown);
	}

	[Test]
	public void Render_ConvertsAdmonitionsBeforeRendering()
	{
		var renderer = new CapturingRenderer();
		var collector = new DiagnosticsCollector([]);
		var fs = new FileSystem();
		var context = new BuildContext(
			collector,
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			TestHelpers.CreateConfigurationContext(fs)
		);
		var renderContext = new ApiRenderContext(
			context,
			new OpenApiDocument(),
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context))
		)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = new LandingNavigationItem("/api/doc/kibana").Index,
			MarkdownRenderer = renderer,
			ApiExplorerLog = null
		};

		_ = ApiMarkdown.Render(renderContext, "Text.\n\n> warn\n> Do not touch the `.kibana` index.\n\nNOTE: Remember this.");

		renderer
			.LastMarkdown
			.Should()
			.Be("Text.\n\n:::{warning}\nDo not touch the `.kibana` index.\n:::\n\n:::{note}\nRemember this.\n:::");
	}

	private sealed class CapturingRenderer : IMarkdownStringRenderer
	{
		public string? LastMarkdown { get; private set; }

		public string Render(string markdown, IFileInfo? source)
		{
			_ = source;
			LastMarkdown = markdown;
			return markdown;
		}
	}
}
