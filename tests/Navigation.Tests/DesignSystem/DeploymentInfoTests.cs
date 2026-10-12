// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site.Components.Chrome.DeploymentInfo;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

public class DeploymentInfoTests
{
	[Test]
	public void PullRequest_DescribesThePullRequestAndLinksIt()
	{
		var model = new DeploymentInfoModel
		{
			Branch = "feature/x",
			Commit = "abc1234",
			Repository = "elastic/docs-builder",
			GitHubRef = "refs/pull/123/merge"
		};

		model.Subtitle.Should().Be("Preview build from pull request #123");
		model.Rows.Select(r => r.Label).Should().Equal("PR", "Branch", "Commit", "Repository");
		model.Rows[0].Value.Should().Be("#123");
		model.Rows[0].Href.Should().Be("https://github.com/elastic/docs-builder/pull/123");
		model.Rows[2].Href.Should().Be("https://github.com/elastic/docs-builder/commit/abc1234");
	}

	[Test]
	public void BranchRef_NamesTheBranchInTheSubtitle()
	{
		var model = new DeploymentInfoModel { Branch = "main", GitHubRef = "refs/heads/main", Repository = "argh" };

		model.Subtitle.Should().Be("Preview build from branch main");
		model.Rows.Single(r => r.Label == "Branch").Href.Should().Be("https://github.com/elastic/argh/tree/main");
	}

	[Test]
	public void LocalBuild_HasNoLinksAndNoPullRequestRow()
	{
		var model = new DeploymentInfoModel { Branch = "main", Commit = "abc1234" };

		model.Subtitle.Should().Be("Preview build");
		model.Rows.Select(r => r.Label).Should().Equal("Branch", "Commit");
		model.Rows.Should().OnlyContain(r => r.Href == null);
	}
}
