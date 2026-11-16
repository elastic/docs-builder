// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Testing.Linked;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>A repository an <c>assembler.yml</c> marks private never reaches a snapshot under its own name.</summary>
public class PrivateRepositoryScrubbingTests
{
	private const string Assembler =
		"""
		references:
		  open-docs:
		  hidden-docs:
		    private: true
		  widget:
		    private: true
		  widget-hosted:
		""";

	[Test]
	public void PrivateNames_Dumped_AreWrittenAsPlaceholders()
	{
		var config = AssemblyConfiguration.Deserialize(Assembler);
		SnapshotScrubbing.AddPrivateRepositories(config.AllPrivateRepositoryNames);

		ModelDump
			.ToText(config.ReferenceRepositories.Keys.ToArray())
			.Should()
			.MatchInlineSnapshot(
				"""
				model:
				  - "open-docs"
				  - "private-repo-1"
				  - "private-repo-2"
				  - "widget-hosted"
				"""
			);
	}

	[Test]
	public void PrivateNames_InUrlsAndPaths_AreWrittenAsPlaceholders()
	{
		var config = AssemblyConfiguration.Deserialize(Assembler);
		SnapshotScrubbing.AddPrivateRepositories(config.AllPrivateRepositoryNames);

		"""
		git@github.com:elastic/hidden-docs.git
		https://github.com/elastic/WIDGET/blob/main/docs/toc.yml
		toc: hidden-docs://reference
		hidden-docs-extra and xwidget and widget-hosted stay
		""".Should().MatchInlineSnapshot(
			"""
			git@github.com:elastic/private-repo-1.git
			https://github.com/elastic/private-repo-2/blob/main/docs/toc.yml
			toc: private-repo-1://reference
			hidden-docs-extra and xwidget and widget-hosted stay
			"""
		);
	}

	[Test]
	public void PlaceholderNames_Added_AreLeftAlone()
	{
		SnapshotScrubbing.AddPrivateRepositories(["private-repo-7", "private-repo-12"]);

		"private-repo-7 private-repo-12".Should().MatchInlineSnapshot("private-repo-7 private-repo-12");
	}

	[Test]
	public void NamesOfOneTest_Added_DoNotRenameAnotherTestsOutput() => "hidden-docs".Should().MatchInlineSnapshot("hidden-docs");
}
