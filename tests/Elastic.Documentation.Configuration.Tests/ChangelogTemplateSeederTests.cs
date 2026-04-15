// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class ChangelogTemplateSeederTests
{
	private const string Template =
		"bundle:\n  resolve: true\n  # changelog-init-bundle-seed\n  # some other comment\n";

	private const string TemplateWindows =
		"bundle:\r\n  resolve: true\r\n  # changelog-init-bundle-seed\r\n  # some other comment\r\n";

	[Fact]
	public void ApplyBundleRepoSeed_GitOwnerAndRepo_SeedsTemplate()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			Template, ownerCli: null, repoCli: null, gitOwner: "elastic", gitRepo: "kibana");

		result.Should().Contain("  owner: elastic\n");
		result.Should().Contain("  repo: kibana\n");
		result.Should().Contain("    - elastic/kibana\n");
		result.Should().NotContain("changelog-init-bundle-seed");
	}

	[Fact]
	public void ApplyBundleRepoSeed_CliOwnerAndRepo_SeedsTemplate()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			Template, ownerCli: "myorg", repoCli: "myrepo", gitOwner: null, gitRepo: null);

		result.Should().Contain("  owner: myorg\n");
		result.Should().Contain("  repo: myrepo\n");
		result.Should().Contain("    - myorg/myrepo\n");
	}

	[Fact]
	public void ApplyBundleRepoSeed_CliOverridesGit()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			Template, ownerCli: "override-owner", repoCli: "override-repo", gitOwner: "elastic", gitRepo: "kibana");

		result.Should().Contain("  owner: override-owner\n");
		result.Should().Contain("  repo: override-repo\n");
		result.Should().Contain("    - override-owner/override-repo\n");
	}

	[Fact]
	public void ApplyBundleRepoSeed_CliRepoOnly_OwnerDefaultsToElastic()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			Template, ownerCli: null, repoCli: "myrepo", gitOwner: null, gitRepo: null);

		result.Should().Contain("  owner: elastic\n");
		result.Should().Contain("  repo: myrepo\n");
		result.Should().Contain("    - elastic/myrepo\n");
	}

	[Fact]
	public void ApplyBundleRepoSeed_CliOwnerOnly_NoRepo_RemovesPlaceholder()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			Template, ownerCli: "myorg", repoCli: null, gitOwner: null, gitRepo: null);

		result.Should().NotContain("changelog-init-bundle-seed");
		result.Should().NotContain("  owner:");
		result.Should().NotContain("  repo:");
	}

	[Fact]
	public void ApplyBundleRepoSeed_NeitherCliNorGit_RemovesPlaceholder()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			Template, ownerCli: null, repoCli: null, gitOwner: null, gitRepo: null);

		result.Should().NotContain("changelog-init-bundle-seed");
		result.Should().NotContain("  owner:");
		result.Should().NotContain("  repo:");
		result.Should().Contain("  # some other comment\n");
	}

	[Fact]
	public void ApplyBundleRepoSeed_WhitespaceCliValues_TreatedAsAbsent()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			Template, ownerCli: "  ", repoCli: "  ", gitOwner: "elastic", gitRepo: "kibana");

		result.Should().Contain("  owner: elastic\n");
		result.Should().Contain("  repo: kibana\n");
	}

	[Fact]
	public void ApplyBundleRepoSeed_WindowsLineEndings_PreservesStyle()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			TemplateWindows, ownerCli: null, repoCli: null, gitOwner: "elastic", gitRepo: "kibana");

		result.Should().Contain("  owner: elastic\r\n");
		result.Should().Contain("  repo: kibana\r\n");
		result.Should().NotContain("  owner: elastic\n  repo:");
	}

	[Fact]
	public void ApplyBundleRepoSeed_MissingPlaceholder_ReturnsContentUnchanged()
	{
		var content = "bundle:\n  resolve: true\n";

		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			content, ownerCli: "elastic", repoCli: "kibana", gitOwner: null, gitRepo: null);

		result.Should().Be(content);
	}

	[Fact]
	public void ApplyBundleRepoSeed_ValuesNeedingYamlQuoting_AreQuoted()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			Template, ownerCli: "my org", repoCli: "my:repo", gitOwner: null, gitRepo: null);

		result.Should().Contain("  owner: \"my org\"\n");
		result.Should().Contain("  repo: \"my:repo\"\n");
		result.Should().Contain("    - \"my org/my:repo\"\n");
	}

	[Fact]
	public void ApplyBundleRepoSeed_CliRepoOverridesGitRepo_KeepsGitOwner()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			Template, ownerCli: null, repoCli: "other-repo", gitOwner: "elastic", gitRepo: "kibana");

		result.Should().Contain("  owner: elastic\n");
		result.Should().Contain("  repo: other-repo\n");
		result.Should().Contain("    - elastic/other-repo\n");
	}

	[Fact]
	public void ApplyBundleRepoSeed_PlaceholderAtEofWithoutNewline_Seeds()
	{
		var content = "bundle:\n  resolve: true\n  # changelog-init-bundle-seed";

		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			content, ownerCli: null, repoCli: null, gitOwner: "elastic", gitRepo: "kibana");

		result.Should().Contain("  owner: elastic");
		result.Should().Contain("  repo: kibana");
		result.Should().NotContain("changelog-init-bundle-seed");
	}

	[Fact]
	public void ApplyBundleRepoSeed_PlaceholderAtEofWithoutNewline_RemovesWhenNoSeed()
	{
		var content = "bundle:\n  resolve: true\n  # changelog-init-bundle-seed";

		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			content, ownerCli: null, repoCli: null, gitOwner: null, gitRepo: null);

		result.Should().Be("bundle:\n  resolve: true\n");
		result.Should().NotContain("changelog-init-bundle-seed");
	}

	[Fact]
	public void ApplyBundleRepoSeed_BackslashInValue_IsEscapedInYaml()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			Template, ownerCli: @"path\org", repoCli: "repo", gitOwner: null, gitRepo: null);

		result.Should().Contain(@"  owner: ""path\\org""");
		result.Should().Contain("  repo: repo\n");
	}

	[Fact]
	public void ApplyBundleRepoSeed_ControlCharsInValue_AreEscapedInYaml()
	{
		var result = ChangelogTemplateSeeder.ApplyBundleRepoSeed(
			Template, ownerCli: "org\tname", repoCli: "repo", gitOwner: null, gitRepo: null);

		result.Should().Contain(@"  owner: ""org\tname""");
	}
}
