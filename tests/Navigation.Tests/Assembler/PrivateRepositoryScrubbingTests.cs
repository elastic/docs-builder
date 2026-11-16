// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Testing.Linked;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class PrivateRepositoryScrubbingTests
{
	private static readonly string[] Names = ["secret-docs", "secret-docs-two", "hidden", "ci-secrets"];

	[Test]
	public void For_NamesWithAHyphen_AreReplacedAsWholeTokensEverywhere()
	{
		var scrub = PrivateRepositoryScrubbing.For(Names);

		var scrubbed = scrub(
			"toc: secret-docs://reference\npath_prefix: reference/secret-docs/rules\nsecret-docs-two:\nnot-secret-docs\nsecret-docs_x\nhttps://github.com/elastic/secret-docs/blob/main/x.md"
		);

		scrubbed.Should().MatchInlineSnapshot(
			"""
			toc: private-repo-3://reference
			path_prefix: reference/private-repo-3/rules
			private-repo-4:
			not-secret-docs
			secret-docs_x
			https://github.com/elastic/private-repo-3/blob/main/x.md
			"""
		);
	}

	[Test]
	public void For_SingleWordNames_AreReplacedOnlyWhereARepositoryCanStand()
	{
		var scrub = PrivateRepositoryScrubbing.For(Names);

		var scrubbed = scrub(
			"hidden://reference\nthe hidden page\nreference/hidden/page\nhttps://github.com/elastic/hidden/tree/main\nhidden-page\nxhidden://y"
		);

		scrubbed.Should().MatchInlineSnapshot(
			"""
			private-repo-2://reference
			the hidden page
			reference/hidden/page
			https://github.com/elastic/private-repo-2/tree/main
			hidden-page
			xhidden://y
			"""
		);
	}

	[Test]
	public void For_PlaceholdersAreNumberedBySortedNameAndLeftAlone()
	{
		var scrub = PrivateRepositoryScrubbing.For([.. Names, "private-repo-1", "private-repo-7"]);

		var scrubbed = scrub("ci-secrets:// hidden:// secret-docs:// secret-docs-two:// private-repo-1:// private-repo-7://");

		scrubbed.Should().MatchInlineSnapshot(
			"private-repo-1:// private-repo-2:// private-repo-3:// private-repo-4:// private-repo-1:// private-repo-7://"
		);
	}

	[Test]
	public void For_NoNames_ReturnsTheTextUnchanged() =>
		PrivateRepositoryScrubbing.For([]).Invoke("secret-docs://x").Should().Be("secret-docs://x");

	[Test]
	public void VolatileValues_ShasAndDates_AreReplaced() =>
		PrivateRepositoryScrubbing
			.VolatileValues(
				"sha 0123456789abcdef0123456789abcdef01234567 short 0123456 on 2026-11-16 at 2026-11-16T10:20:30.123Z and 2026-11-16 10:20"
			)
			.Should()
			.MatchInlineSnapshot("sha {sha} short 0123456 on {date} at {date} and {date}");

	[Test]
	public void FrozenBaseline_ContainsNoPrivateRepositoryOfTheShippedConfig()
	{
		var root = Paths.GetSolutionDirectory() ?? throw new InvalidOperationException("solution root not found");
		var shipped = AssemblyConfiguration.Deserialize(File.ReadAllText(Path.Join(root.FullName, "config", "assembler.yml")));
		var scrub = PrivateRepositoryScrubbing.For(shipped.AllPrivateRepositoryNames);
		shipped.AllPrivateRepositoryNames.Should().NotBeEmpty("the check below would pass vacuously");

		var files = Directory
			.EnumerateFiles(Path.Join(root.FullName, "tests", "Navigation.Tests", "Assembler", "Baseline"))
			.Concat(
				Directory.EnumerateFiles(
					Path.Join(root.FullName, "tests", "Navigation.Tests", ".snapshots", "Assembler"),
					"*",
					SearchOption.AllDirectories
				)
			)
			.ToArray();
		files.Should().NotBeEmpty();

		var leaking = files.Count(f => scrub(File.ReadAllText(f)) != File.ReadAllText(f));

		// the names are not part of the message on purpose
		leaking.Should().Be(0, "a frozen copy or a verified file names a private repository");
	}
}
