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
}
