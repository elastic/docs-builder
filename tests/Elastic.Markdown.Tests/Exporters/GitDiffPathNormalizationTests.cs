// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Exporters.GitDiff;

namespace Elastic.Markdown.Tests.Exporters;

public class GitDiffPathNormalizationTests
{
	[Theory]
	[InlineData("docs/guides/start.md", "docs", "guides/start.md")]
	[InlineData("guides/start.md", "", "guides/start.md")]
	[InlineData("docs/reference/index.md", "docs", "reference/index.md")]
	public void TryToDocsetRelative_StripsDocsetPrefix(string repoPath, string prefix, string expected)
	{
		GitDiffPathNormalization.TryToDocsetRelative(repoPath, prefix, out var relative).Should().BeTrue();
		relative.Should().Be(expected);
	}

	[Fact]
	public void TryToDocsetRelative_RejectsPathsOutsideDocset()
	{
		GitDiffPathNormalization.TryToDocsetRelative("docs/guides/start.md", "docs", out _).Should().BeTrue();
		GitDiffPathNormalization.TryToDocsetRelative("other/page.md", "docs", out _).Should().BeFalse();
		GitDiffPathNormalization.TryToDocsetRelative("docs", "docs", out var root).Should().BeTrue();
		root.Should().BeEmpty();
	}

	[Fact]
	public void IsMarkdownPagePath_SkipsSnippetFolders()
	{
		GitDiffPathNormalization.IsMarkdownPagePath("_snippets/foo.md").Should().BeFalse();
		GitDiffPathNormalization.IsMarkdownPagePath("guides/_snippets/foo.md").Should().BeFalse();
		GitDiffPathNormalization.IsMarkdownPagePath("guides/page.md").Should().BeTrue();
	}

	[Fact]
	public void Normalize_ConvertsWindowsSlashesAndTrimsDotSegments()
	{
		GitDiffPathNormalization.Normalize(@".\docs\page.md").Should().Be("docs/page.md");
	}
}
