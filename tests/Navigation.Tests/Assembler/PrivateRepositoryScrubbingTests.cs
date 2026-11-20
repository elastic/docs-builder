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
	public void Forbidden_SingleWordNames_AreForbiddenOnlyAsTheFormsAnExactValueCanCover()
	{
		var forbidden = PrivateRepositoryScrubbing.Forbidden(["widget", "secret-docs"]);

		forbidden.Should().Contain([
			"secret-docs",
			"SECRET-DOCS",
			"widget://",
			"elastic/widget/",
			"elastic/WIDGET.git",
			"elastic/widget\""
		]);
		forbidden.Should().NotContain("widget");
	}

	[Test]
	[Arguments("", true)]
	[Arguments("\n", true)]
	[Arguments(".", true)]
	[Arguments(":", true)]
	[Arguments(" #", true)]
	[Arguments(")", true)]
	[Arguments("#readme", true)]
	[Arguments("?x", true)]
	[Arguments("\"", true)]
	[Arguments("/", true)]
	[Arguments("-hosted", false)]
	[Arguments("_x", false)]
	[Arguments("x", false)]
	public void LeakingLines_SingleWordAfterElasticSlash_IsFoundUpToAWordCharacterOrHyphen(string after, bool leaks)
	{
		var lines = PrivateRepositoryScrubbing.LeakingLines($"first\nrepo: elastic/widget{after}", ["widget"]);

		(lines.Count > 0).Should().Be(leaks);
	}

	[Test]
	public void LeakingLines_OtherCaseAndTokens_AreFound()
	{
		var names = new[] { "widget", "secret-docs" };

		PrivateRepositoryScrubbing
			.LeakingLines("a\nhttps://github.com/elastic/WIDGET/x\nWidget://y\nthe widget page\nSecret-Docs and not-secret-docs", names)
			.Should()
			.Equal(2, 3, 5);
	}

	[Test]
	public void FrozenBaseline_AFileThatLeaks_IsCountedWithoutItsName()
	{
		var folder = Directory.CreateTempSubdirectory("leak-guard").FullName;
		try
		{
			File.WriteAllText(Path.Join(folder, "clean.txt"), "repo: elastic/widget-hosted\n");
			File.WriteAllText(Path.Join(folder, "leaks.txt"), "repo: elastic/widget");

			CountLeakingFiles(Directory.EnumerateFiles(folder), ["widget"]).Should().Be(1);
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	private static int CountLeakingFiles(IEnumerable<string> files, IEnumerable<string> names) =>
		files.Count(f => PrivateRepositoryScrubbing.LeakingLines(File.ReadAllText(f), names).Count > 0);

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

		var leaking = CountLeakingFiles(files, shipped);

		// the names are not part of the message on purpose
		leaking.Should().Be(0, "a frozen copy or a verified file names a private repository");
	}

	[Test]
	public void Options_ASnapshotThatNamesAPrivateRepository_FailsOnTheForbidCheckAndWritesNoReceivedFile()
	{
		const string Value = "widget-secret-synthetic";
		var options = new SnapshotOptions().Forbid([Value]);

		var (message, receivedFiles) = ForbidProbe.Run($"first line\nrepository {Value}\n", Value, options);

		message.Should().NotBeNull("the text is its own snapshot, so only the forbid check can fail it");
		message.Should().Contain("2", "the message gives the line of the forbidden value");
		message!.Contains(Value, StringComparison.Ordinal).Should().BeFalse("the message names the value");
		receivedFiles.Should().Be(0, "a forbidden value must not reach a received file");
	}
}
