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
				  - "private-repo_1"
				  - "private-repo_2"
				  - "private-repo_2-hosted"
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
		https://github.com/elastic/widget/blob/main/docs/toc.yml
		toc: hidden-docs://reference
		xwidget and widget_x stay, widget-hosted is cut at the hyphen
		""".Should().MatchInlineSnapshot(
			"""
			git@github.com:elastic/private-repo_1.git
			https://github.com/elastic/private-repo_2/blob/main/docs/toc.yml
			toc: private-repo_1://reference
			xwidget and widget_x stay, private-repo_2-hosted is cut at the hyphen
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
	public void ShippedPrivateNames_AreForbiddenInEverySnapshot() =>
		PrivateRepositoryGuard.Forbidden.Should().NotBeEmpty("the guard would pass vacuously");

	[Test]
	public void NamesOfOneTest_Added_DoNotRenameAnotherTestsOutput() => "hidden-docs".Should().MatchInlineSnapshot("hidden-docs");
}
