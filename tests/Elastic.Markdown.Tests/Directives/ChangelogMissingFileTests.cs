// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Tests that the changelog directive emits errors (not warnings) when a bundle contains
/// unresolved file references that cannot be found on disk.
/// This ensures builds fail fast rather than silently omitting changelog entries.
/// </summary>
public class ChangelogMissingFileReferenceTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMissingFileReferenceTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") =>
		// Bundle references a file that does not exist on disk (unresolved reference)
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- file:
			    name: 1234-missing-entry.yaml
			    checksum: abc123
			"""));// Intentionally NOT adding docs/changelog/1234-missing-entry.yaml

	[Fact]
	public void EmitsErrorForMissingReferencedFile() =>
		Collector.Diagnostics.Should().ContainSingle(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("1234-missing-entry.yaml") &&
			d.Message.Contains("not found"));

	[Fact]
	public void ErrorIsNotAWarning() =>
		Collector.Diagnostics.Should().NotContain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("1234-missing-entry.yaml"));
}

/// <summary>
/// Tests that the changelog directive does NOT error when a bundle has
/// inline (resolved) entries — files being absent from disk is irrelevant.
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

/// <summary>
/// Tests that the changelog directive correctly loads resolved file references
/// when the referenced files exist.
/// </summary>
public class ChangelogFileReferenceResolvesCorrectlyTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogFileReferenceResolvesCorrectlyTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		// Bundle references a file that DOES exist
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- file:
			    name: 1234-existing-entry.yaml
			    checksum: placeholder
			"""));

		// Add the referenced file
		FileSystem.AddFile("docs/changelog/1234-existing-entry.yaml", new MockFileData(
			// language=yaml
			"""
			title: An existing feature
			type: feature
			products:
			- product: elasticsearch
			  target: 9.3.0
			prs:
			- "1234"
			"""));
	}

	[Fact]
	public void HasNoDiagnostics() =>
		Collector.Diagnostics.Should().BeEmpty();

	[Fact]
	public void LoadsEntry() =>
		Block!.LoadedBundles.Should().ContainSingle(b => b.Entries.Count == 1);
}
