// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Tests that the changelog directive emits errors (not warnings) when a bundle entry has no
/// inline content. Bundles are always self-contained: entry files are never read from disk,
/// so a file-reference-only entry is invalid and builds fail fast rather than silently
/// omitting changelog entries.
/// </summary>
public class ChangelogEntryWithoutInlineContentTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogEntryWithoutInlineContentTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		// Bundle entry carries only file provenance — no inline title/type
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- file:
			    name: 1234-referenced-entry.yaml
			    checksum: abc123
			"""));

		// Even when the referenced file exists on disk it must never be read
		FileSystem.AddFile("docs/changelog/1234-referenced-entry.yaml", new MockFileData(
			// language=yaml
			"""
			title: A referenced feature
			type: feature
			products:
			- product: elasticsearch
			  target: 9.3.0
			prs:
			- "1234"
			"""));
	}

	[Fact]
	public void EmitsErrorNamingBundleAndEntry() =>
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("9.3.0.yaml") &&
			d.Message.Contains("1234-referenced-entry.yaml") &&
			d.Message.Contains("no inline content"));

	[Fact]
	public void ErrorIsNotAWarning() =>
		Collector.Diagnostics.Should().NotContain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("1234-referenced-entry.yaml"));

	[Fact]
	public void NeverLoadsTheReferencedFile() =>
		Block!.LoadedBundles.Should().ContainSingle(b => b.Entries.Count == 0);
}

/// <summary>
/// Tests that the changelog directive loads bundles with inline (resolved) entries without
/// diagnostics — files being absent from disk is irrelevant.
/// </summary>
public class ChangelogInlineEntriesNoErrorTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogInlineEntriesNoErrorTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") =>
		// Bundle has fully inline/resolved entry — no file reference needed
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Inline feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "999"
			"""));

	[Fact]
	public void HasNoDiagnostics() =>
		Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void LoadsEntries() =>
		Block!.LoadedBundles.Should().ContainSingle(b => b.Entries.Count == 1);
}
