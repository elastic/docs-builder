// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Tests.Changelogs;

public class PreviewProblemMergeTests
{
	private static PreviewProblem Problem(string product, string repo, int number, string release = "head") =>
		new()
		{
			Product = product,
			Release = release,
			Kind = PreviewProblemKind.MissingTypeLabel,
			Owner = "elastic",
			Repo = repo,
			Number = number
		};

	[Test]
	public void Merge_ARefreshedDraProduct_IsReplacedAndTheOthersStay()
	{
		var existing = new[] { Problem("kibana", "kibana", 1), Problem("beats", "beats", 2) };
		var fresh = new[] { Problem("kibana", "kibana", 3) };

		var merged = PreviewProblemMerge.Merge(existing, fresh, ["kibana"], []);

		merged.Select(p => (p.Product, p.Number)).Should().Equal(("beats", 2), ("kibana", 3));
	}

	[Test]
	public void Merge_OneServiceOfASharedProduct_LeavesTheProblemsOfTheOtherService()
	{
		var existing = new[]
		{
			Problem("cloud-serverless", "kibana", 1),
			Problem("cloud-serverless", "elasticsearch-serverless", 2),
			Problem("cloud-serverless", "elasticsearch", 3),
			Problem("cloud-hosted", "cloud", 4)
		};
		var fresh = new[] { Problem("cloud-serverless", "kibana", 5) };

		var merged = PreviewProblemMerge.Merge(existing, fresh, [], [("cloud-serverless", "kibana")]);

		merged.Select(p => p.Number).Should().BeEquivalentTo([2, 3, 4, 5]);
	}

	[Test]
	public void Merge_ARefreshedServiceThatFoundNothing_DropsItsEarlierProblems()
	{
		var existing = new[] { Problem("cloud-hosted", "cloud", 4) };

		PreviewProblemMerge.Merge(existing, [], [], [("cloud-hosted", "cloud")]).Should().BeEmpty();
	}

	[Test]
	public void Merge_NothingRefreshed_KeepsEverythingOnDisk()
	{
		var existing = new[] { Problem("kibana", "kibana", 1) };

		PreviewProblemMerge.Merge(existing, [], [], []).Should().Equal(existing);
	}

	[Test]
	public void PullRequestCount_ApullRequestWithTwoProblemsOrTwoReleases_CountsOnce()
	{
		var log = new PreviewProblemLog();
		log.Add(
			"kibana",
			"head",
			new GitRangePrProblem(1, "https://github.com/elastic/kibana/pull/1", "t", PreviewProblemKind.MissingTypeLabel, string.Empty)
		);
		log.Add(
			"kibana",
			"head",
			new GitRangePrProblem(1, "https://github.com/elastic/kibana/pull/1", "t", PreviewProblemKind.MissingAreaLabel, string.Empty)
		);
		log.Add(
			"kibana",
			"staging",
			new GitRangePrProblem(1, "https://github.com/elastic/kibana/pull/1", "t", PreviewProblemKind.MissingTypeLabel, string.Empty)
		);
		log.Add(
			"kibana",
			"head",
			new GitRangePrProblem(2, "https://github.com/elastic/kibana/pull/2", "t", PreviewProblemKind.MissingTypeLabel, string.Empty)
		);

		(log.PullRequestCount, log.Count).Should().Be((2, 4));
	}
}
