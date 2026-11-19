// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Publishing;
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
		var config = AssemblyLoader.Deserialize(Assembler);
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
		var config = AssemblyLoader.Deserialize(Assembler);
		SnapshotScrubbing.AddPrivateRepositories(config.AllPrivateRepositoryNames);

		"""
		git@github.com:elastic/hidden-docs.git
		https://github.com/elastic/WIDGET/blob/main/docs/toc.yml
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
	public void ShippedPrivateNames_AreForbiddenInEverySnapshotAndWriteNoReceivedFile()
	{
		PrivateRepositoryGuard.Forbidden.Should().NotBeEmpty("the guard would pass vacuously");
		var name = PrivateRepositoryGuard.Forbidden.First(n => n.Contains('-'));

		var (message, receivedFiles) = ForbidProbe.Run($"first line\nrepository {name}\n", name);

		message.Should().NotBeNull("the text is its own snapshot, so only the default forbid check can fail it");
		message.Should().Contain("2", "the message gives the line of the forbidden value");
		message!.Contains(name, StringComparison.Ordinal).Should().BeFalse("the message names a private repository");
		receivedFiles.Should().Be(0, "a forbidden value must not reach a received file");
	}

	[Test]
	public void NamesOfOneTest_Added_DoNotRenameAnotherTestsOutput() => "hidden-docs".Should().MatchInlineSnapshot("hidden-docs");
}
