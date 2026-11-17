// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Testing.Linked;
using Nullean.Snapshots;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class PrivateRepositoryScrubbingTests
{
	private static readonly string[] Names = ["secret-docs", "secret-docs-two", "hidden", "ci-secrets"];

	internal static IReadOnlySet<string> ShippedPrivateRepositories()
	{
		var root = Paths.GetSolutionDirectory() ?? throw new InvalidOperationException("solution root not found");
		return AssemblyConfiguration.Deserialize(
			File.ReadAllText(Path.Join(root.FullName, "config", "assembler.yml"))
		).AllPrivateRepositoryNames;
	}

	private static List<int> LinesWith(string text, IReadOnlyList<string> values) =>
		[
			.. text
				.ReplaceLineEndings("\n")
				.Split('\n')
				.Select((line, i) => (line, number: i + 1))
				.Where(l => values.Any(v => l.line.Contains(v, StringComparison.Ordinal)))
				.Select(l => l.number)
		];

	[Test]
	public void Scrub_Names_AreNumberedByFirstAppearanceAsWholeWords()
	{
		var scrub = PrivateRepositoryScrubbing.Scrub(Names);

		var scrubbed = scrub(
			"toc: secret-docs://reference\npath_prefix: reference/secret-docs/rules\nsecret-docs-two:\nnot-secret-docs\nsecret-docs_x\nhttps://github.com/elastic/secret-docs/blob/main/x.md"
		);

		scrubbed.Should().MatchInlineSnapshot(
			"""
			toc: private-repo_1://reference
			path_prefix: reference/private-repo_1/rules
			private-repo_2:
			not-private-repo_1
			secret-docs_x
			https://github.com/elastic/private-repo_1/blob/main/x.md
			"""
		);
	}

	[Test]
	public void Scrub_PlaceholdersAndNoNames_LeaveTheTextAlone()
	{
		PrivateRepositoryScrubbing
			.Scrub([.. Names, "private-repo-1", "private-repo-7"])("private-repo-1:// private-repo-7://")
			.Should()
			.MatchInlineSnapshot("private-repo-1:// private-repo-7://");
		PrivateRepositoryScrubbing.Scrub([]).Invoke("secret-docs://x").Should().Be("secret-docs://x");
	}

	[Test]
	public void Forbidden_SingleWordNames_AreForbiddenOnlyWhereARepositoryCanStand()
	{
		var forbidden = PrivateRepositoryScrubbing.Forbidden(["hidden", "secret-docs"]);

		var lines = LinesWith(
			"hidden://reference\nthe hidden page\nreference/hidden/page\nhttps://github.com/elastic/hidden/tree/main\nhidden-page\nxhidden://y\nsecret-docs",
			forbidden
		);

		lines.Should().Equal(1, 4, 6, 7);
	}

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
		var shipped = ShippedPrivateRepositories();
		shipped.Should().NotBeEmpty("the check below would pass vacuously");
		var forbidden = PrivateRepositoryScrubbing.Forbidden(shipped);
		forbidden.Should().NotBeEmpty("the check below would pass vacuously");

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

		var leaking = files.Count(f => LinesWith(File.ReadAllText(f), forbidden).Count > 0);

		// the names are not part of the message on purpose
		leaking.Should().Be(0, "a frozen copy or a verified file names a private repository");
	}

	[Test]
	public void Options_ASnapshotThatNamesAPrivateRepository_FailsWithoutShowingTheName()
	{
		var shipped = PrivateRepositoryScrubbing.Forbidden(ShippedPrivateRepositories()).First(n => n.Contains('-'));
		var options = new SnapshotOptions().Forbid([shipped]);

		var message = string.Empty;
		try
		{
			$"first line\nrepository {shipped}\n".Should().MatchSnapshot(options: options);
		}
		catch (Exception e)
		{
			message = e.Message;
		}

		message.Should().NotBeEmpty("the snapshot must fail");
		// a failing check must not repeat the name either
		message.Contains(shipped, StringComparison.Ordinal).Should().BeFalse("the message names a private repository");
	}
}
