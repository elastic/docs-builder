// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.ReleaseSchedule;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>The handoff from <c>release unified bundle</c> to the bundler: what the bundler is told for one product.</summary>
public class UnifiedBundleArgumentsTests
{
	[Test]
	public void Create_Range_PassesTheCommitRangeAndTheRepositoryToTheBundler()
	{
		var range = new ProductBundleRange("elastic-agent", "elastic-agent", "9.5.5", "aaaa1111", "bbbb2222");

		var arguments = UnifiedBundleArguments.Create(
			range,
			"agent_release",
			"/work/bundles/elastic-agent/config.yml",
			"/work/bundles/elastic-agent"
		);

		arguments.StartGitRef.Should().Be("aaaa1111");
		arguments.EndGitRef.Should().Be("bbbb2222");
		arguments.Owner.Should().Be("elastic");
		arguments.Repo.Should().Be("elastic-agent");
	}

	[Test]
	public void Create_Range_UsesTheProfileOfTheProductWithTheVersionAsItsArgument()
	{
		var range = new ProductBundleRange("kibana", "kibana", "9.6.0", "aaaa1111", "bbbb2222");

		var arguments = UnifiedBundleArguments.Create(range, "kibana_release", "/work/config.yml", "/work/bundles/kibana");

		arguments.Profile.Should().Be("kibana_release");
		arguments.ProfileArgument.Should().Be("9.6.0");
		arguments.Config.Should().Be("/work/config.yml");
		arguments.OutputDirectory.Should().Be("/work/bundles/kibana");
	}

	[Test]
	public void Create_Range_LeavesEveryOtherFilterUnset()
	{
		var range = new ProductBundleRange("kibana", "kibana", "9.6.0", "aaaa1111", "bbbb2222");

		var arguments = UnifiedBundleArguments.Create(range, "kibana_release", "/work/config.yml", "/work/bundles/kibana");

		arguments.All.Should().BeFalse();
		arguments.Prs.Should().BeNull();
		arguments.Issues.Should().BeNull();
		arguments.Files.Should().BeNull();
		arguments.InputProducts.Should().BeNull();
	}
}
