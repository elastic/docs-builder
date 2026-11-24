// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Tests.Changelogs;

public class PreviewProblemLogTests
{
	private static readonly string[] Allow = ["elastic/kibana"];

	private static GitRangePrProblem Problem(
		string repo,
		int number,
		PreviewProblemKind kind = PreviewProblemKind.MissingProductLabel,
		string title = "A title"
	) => new(number, $"https://github.com/elastic/{repo}/pull/{number}", title, kind, "@Product:ECE, @Product:ECH");

	[Test]
	public void ToProblems_PublicRepository_KeepsTheLinkAndTheTitle()
	{
		var log = new PreviewProblemLog();
		log.Add("kibana", "head", Problem("kibana", 7, title: "Public title"));

		var problem = log.ToProblems(Allow).Should().ContainSingle().Which;

		problem.Url.Should().Be("https://github.com/elastic/kibana/pull/7");
		problem.Title.Should().Be("Public title");
		(problem.Owner, problem.Repo, problem.Number).Should().Be(("elastic", "kibana", 7));
	}

	[Test]
	public void ToProblems_PrivateRepository_DropsTheLinkAndTheTitle()
	{
		var log = new PreviewProblemLog();
		log.Add("cloud-hosted", "head", Problem("cloud", 161048, title: "Secret title"));

		var problem = log.ToProblems(Allow).Should().ContainSingle().Which;

		problem.Url.Should().BeNull();
		problem.Title.Should().BeNull();
		(problem.Owner, problem.Repo, problem.Number).Should().Be(("elastic", "cloud", 161048));
	}

	[Test]
	public void ToProblems_UrlThatIsNotAPullRequest_IsTreatedAsPrivateAndKeepsTheNumber()
	{
		var log = new PreviewProblemLog();
		log.Add(
			"kibana",
			"head",
			new GitRangePrProblem(5, "https://example.com/not-github", "Hidden", PreviewProblemKind.MissingTypeLabel, string.Empty)
		);

		var problem = log.ToProblems(Allow).Should().ContainSingle().Which;

		(problem.Url, problem.Title, problem.Owner, problem.Repo, problem.Number).Should().Be((null, null, string.Empty, string.Empty, 5));
	}

	[Test]
	public void ToProblems_AllowlistIgnoresCase()
	{
		var log = new PreviewProblemLog();
		log.Add("kibana", "head", Problem("kibana", 1));

		log.ToProblems(["Elastic/Kibana"]).Single().Url.Should().NotBeNull();
	}

	[Test]
	public void Add_TheSamePullRequestTwiceForARelease_IsRecordedOnceButOtherReleasesAndKindsAreKept()
	{
		var log = new PreviewProblemLog();
		log.Add("kibana", "head", Problem("kibana", 1));
		log.Add("kibana", "head", Problem("kibana", 1));
		log.Add("kibana", "staging", Problem("kibana", 1));
		log.Add("kibana", "head", Problem("kibana", 1, PreviewProblemKind.MissingTypeLabel));

		log.Count.Should().Be(3);
	}

	[Test]
	public void ToProblems_AreOrderedByProductReleaseKindAndNumber()
	{
		var log = new PreviewProblemLog();
		log.Add("kibana", "staging", Problem("kibana", 9));
		log.Add("cloud-hosted", "head", Problem("cloud", 3, PreviewProblemKind.MissingTypeLabel));
		log.Add("cloud-hosted", "head", Problem("cloud", 2));
		log.Add("cloud-hosted", "head", Problem("cloud", 1));

		log
			.ToProblems(Allow)
			.Select(p => (p.Product, p.Release, p.Kind, p.Number))
			.Should()
			.Equal(
				("cloud-hosted", "head", PreviewProblemKind.MissingProductLabel, 1),
				("cloud-hosted", "head", PreviewProblemKind.MissingProductLabel, 2),
				("cloud-hosted", "head", PreviewProblemKind.MissingTypeLabel, 3),
				("kibana", "staging", PreviewProblemKind.MissingProductLabel, 9)
			);
	}

	[Test]
	public void File_RoundTripsTheProblemsAndNeverHoldsAPrivateTitle()
	{
		var fileSystem = new MockFileSystem();
		var log = new PreviewProblemLog();
		log.Add("kibana", "head", Problem("kibana", 7, title: "Public title"));
		log.Add("cloud-hosted", "head", Problem("cloud", 161048, title: "Secret title"));
		var problems = log.ToProblems(Allow);

		PreviewProblemsFile.Write(fileSystem, "/bundles", problems);

		fileSystem
			.File
			.ReadAllText("/bundles/_diagnostics/preview-problems.json")
			.Should()
			.NotContain("Secret title")
			.And
			.NotContain("pull/161048");
		PreviewProblemsFile.Read(fileSystem, "/bundles").Should().BeEquivalentTo(problems, options => options.WithStrictOrdering());
	}

	[Test]
	public void File_WithoutAFile_HasNoProblems() => PreviewProblemsFile.Read(new MockFileSystem(), "/bundles").Should().BeEmpty();
}
