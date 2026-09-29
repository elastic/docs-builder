// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Changelog;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Tests for <see cref="ChangelogConfigurationLoader.ReadBundleProfileNames"/>, which the DRA bundle
/// command calls to discover the profile name from a product's <c>changelog.yml</c> fetched from GitHub.
/// </summary>
public class ReadBundleProfileNamesTests
{
	[Fact]
	public void ReadBundleProfileNames_WithSingleProfile_ReturnsThatName()
	{
		// language=yaml
		var yaml =
			"""
			bundle:
			  profiles:
			    kibana-release:
			      products: "kibana {version} *"
			""";

		var names = ChangelogConfigurationLoader.ReadBundleProfileNames(yaml);

		names.Should().ContainSingle().Which.Should().Be("kibana-release");
	}

	[Fact]
	public void ReadBundleProfileNames_WithMultipleProfiles_ReturnsAllNamesInOrder()
	{
		// language=yaml
		var yaml =
			"""
			bundle:
			  profiles:
			    kibana-release:
			      products: "kibana {version} ga"
			    kibana-prerelease:
			      products: "kibana {version} beta rc"
			    serverless-release:
			      products: "cloud-serverless {version} *"
			""";

		var names = ChangelogConfigurationLoader.ReadBundleProfileNames(yaml);

		names.Should().Equal("kibana-release", "kibana-prerelease", "serverless-release");
	}

	[Fact]
	public void ReadBundleProfileNames_WithNoBundleSection_ReturnsEmpty()
	{
		// A changelog.yml that is valid but has no bundle section at all.
		// language=yaml
		var yaml = """
			filename: pr
			""";

		var names = ChangelogConfigurationLoader.ReadBundleProfileNames(yaml);

		names.Should().BeEmpty();
	}

	[Fact]
	public void ReadBundleProfileNames_WithBundleSectionButNoProfiles_ReturnsEmpty()
	{
		// language=yaml
		var yaml = """
			bundle:
			  directory: changelogs/
			  use_local_changelogs: true
			""";

		var names = ChangelogConfigurationLoader.ReadBundleProfileNames(yaml);

		names.Should().BeEmpty();
	}

	[Fact]
	public void ReadBundleProfileNames_WithEmptyProfilesMap_ReturnsEmpty()
	{
		// language=yaml
		var yaml = """
			bundle:
			  profiles: {}
			""";

		var names = ChangelogConfigurationLoader.ReadBundleProfileNames(yaml);

		names.Should().BeEmpty();
	}
}
