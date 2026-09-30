// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Tests.Golden;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Configuration.Tests;

public class TableOfContentsLoadingTests
{
	[Test]
	public void Deserialize_CrossLinkWithHidden_IsAHiddenCrossLink()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - crosslink: docs-content://get-started/introduction.md
		               title: Introduction
		               hidden: true
		           """;

		var docset = ConfigurationFileProvider.Deserializer.Deserialize<DocumentationSetDto>(yaml);

		var crossLink = docset.TocEntries.Should().ContainSingle().Which.Should().BeOfType<CrossLinkEntryDto>().Subject;
		crossLink.Hidden.Should().BeTrue();
		crossLink.CrossLinkUri.ToString().Should().Be("docs-content://get-started/introduction.md");
	}

	[Test]
	public void LoadAndResolve_NestedTocIncludingItsAncestor_ReportsTheCycle()
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/docset.yml", new MockFileData("project: test\ntoc:\n  - file: index.md\n  - toc: guides\n"));
		fileSystem.AddFile("/docs/guides/toc.yml", new MockFileData("toc:\n  - file: index.md\n  - toc: ../guides\n"));
		var collector = new CapturingDiagnosticsCollector();

		var docset = DocumentationSetLoader.LoadAndResolve(
			collector,
			fileSystem.FileInfo.New("/docs/docset.yml"),
			new ScopedFileSystem(fileSystem, "/docs")
		);

		var cycle = collector.Diagnostics.Should().ContainSingle().Subject;
		cycle.Severity.Should().Be(Severity.Error);
		cycle
			.Message
			.Should()
			.Match(
				"Table of contents '*' includes itself: *guides*toc.yml -> *guides*toc.yml. Remove the 'toc:' entry that closes the loop."
			);
		docset.TableOfContents.Descendants().OfCase<IsolatedTableOfContentsRef>().Should().HaveCount(2);
	}
}
