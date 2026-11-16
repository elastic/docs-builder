// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Documentation.Testing.Linked;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Documentation.Tests.Testing;

public class SnapshotHelpersTests
{
	[Test]
	public void ToSnapshotText_NoDiagnostics_IsEmpty() => new List<Diagnostic>().ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public void ToSnapshotText_MixedSeverities_OneLineEachInOrder()
	{
		var diagnostics = new List<Diagnostic>
		{
			new() { Severity = Severity.Error, File = "a.md", Line = 3, Message = "bad link" },
			new() { Severity = Severity.Warning, File = "b.md", Message = "no line" },
			new() { Severity = Severity.Hint, File = "c.md", Line = 1, Message = "consider" }
		};
		diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			error a.md:3 bad link
			warning b.md no line
			hint c.md:1 consider
			"""
			);
	}

	[Test]
	public void MockFileSystemDump_SortsByPathAndNormalisesLineEndings()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/out/b/two.md", new MockFileData("two\r\nlines"));
		fs.AddFile("/out/a.md", new MockFileData("one\n"));
		MockFileSystemDump
			.ToText(fs, "/out")
			.Should()
			.MatchInlineSnapshot(
				"""
			=== a.md ===
			one
			=== b/two.md ===
			two
			lines
			"""
			);
	}

	[Test]
	public void DocsHtmlSection_DropsFixtureHeadingAndHtmxAttributes() =>
		"""
		<html><body><section id="elastic-docs-v3"><h1>Test Document</h1><p><a href="/x" hx-get="/x" preload="true">x</a></p></section><footer>nav</footer></body></html>
		""".Should().MatchInlineHtmlSnapshot(
			"""
			<p><a href="/x">x</a></p>
			""",
			DocsHtml.Section()
		);

	[Test]
	public void PathScrubber_WindowsRoot_WritesTheRestWithForwardSlashesAndKeepsTheLine() =>
		SnapshotScrubbing
			.Under(@"D:\a\docs-builder\docs-builder", "{repo}")(@"error D:\a\docs-builder\docs-builder\docs\index.md:3 bad link")
			.Should()
			.MatchInlineSnapshot("error {repo}/docs/index.md:3 bad link");

	[Test]
	public void PathScrubber_UnixRootInsideText_LeavesOtherPathsAlone() =>
		SnapshotScrubbing
			.Under("/work/repo/", "{repo}")("see /work/repo/docs/a.md and /elsewhere/b.md")
			.Should()
			.MatchInlineSnapshot("see {repo}/docs/a.md and /elsewhere/b.md");

	[Test]
	public void TempScrubber_GuidFolderName_IsReplacedSoTheSnapshotIsStable() =>
		SnapshotScrubbing
			.Under("/tmp", "{temp}", new System.Text.RegularExpressions.Regex("[0-9a-fA-F]{32}"))(
				"error /tmp/diff-validate-test-1d248de3468f4d58849398ff8b320802/docs/redirects.yml Missing"
			)
			.Should()
			.MatchInlineSnapshot("error {temp}/diff-validate-test-{id}/docs/redirects.yml Missing");

	[Test]
	public void ToSnapshotText_WindowsPaths_ReadLikeUnixPaths()
	{
		var diagnostics = new List<Diagnostic>
		{
			new()
			{
				Severity = Severity.Error,
				File = @"C:\docs\section-b\toc.yml",
				Message = @"`sub-requirements2` does not exist in testing\req.md."
			},
			new() { Severity = Severity.Error, File = "C:/docs/docset.yml", Line = 4, Message = "see https://elastic.co/docs and D:/x" },
			new() { Severity = Severity.Warning, File = @"/docs\docset.yml", Message = "mixed" }
		};
		diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			error /docs/section-b/toc.yml `sub-requirements2` does not exist in testing/req.md.
			error /docs/docset.yml:4 see https://elastic.co/docs and /x
			warning /docs/docset.yml mixed
			"""
			);
	}

	[Test]
	public void ToSnapshotText_FirstLineOnly_DropsTheStackTrace()
	{
		var diagnostics = new List<Diagnostic>
		{
			new()
			{
				Severity = Severity.Error,
				File = "index.md",
				Line = 3,
				Message =
					"registry could not be read\nSystem.IO.FileNotFoundException: Could not find file 'x'.\n   at Some.Method() in /src/File.cs:line 209"
			}
		};
		diagnostics.ToSnapshotText(firstLineOnly: true).Should().MatchInlineSnapshot("error index.md:3 registry could not be read");
	}

	[Test]
	public void PathScrubber_RootAfterDiagnosticsSnapshotNormalisedIt_StillReadsAsRepo()
	{
		// what a Windows run writes for D:\a\docs-builder\docs-builder\docs\index.md once the drive letter and backslashes are gone
		var scrub = SnapshotScrubbing.Under(@"D:\a\docs-builder\docs-builder", "{repo}");
		scrub("error /a/docs-builder/docs-builder/docs/index.md:3 bad").Should().MatchInlineSnapshot("error {repo}/docs/index.md:3 bad");
		scrub(@"error D:\a\docs-builder\docs-builder\docs\index.md:3 bad").Should().MatchInlineSnapshot("error {repo}/docs/index.md:3 bad");
	}

	[Test]
	public void ContentHashScrubber_ReplacesTheAssetCacheBustingHash_OnlyThere() =>
		SnapshotScrubbing
			.ContentHash("""<img src="/docs/_static/logo.svg?v=19bc4040eda89518"> and ?v=1.2 and id=19bc4040eda89518""")
			.Should()
			.MatchInlineSnapshot("""<img src="/docs/_static/logo.svg?v={hash}"> and ?v=1.2 and id=19bc4040eda89518""");
}
