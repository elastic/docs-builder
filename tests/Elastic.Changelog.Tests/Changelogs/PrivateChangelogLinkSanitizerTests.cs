// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Tests.Changelogs;

public class PrivateChangelogLinkSanitizerTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	[Fact]
	public void TryGetGitHubRepo_FullUrl_ParsesOwnerRepo()
	{
		var ok = ChangelogTextUtilities.TryGetGitHubRepo(
			"https://github.com/elastic/kibana-team/pull/456",
			"elastic",
			"elasticsearch",
			out var owner,
			out var repo);

		ok.Should().BeTrue();
		owner.Should().Be("elastic");
		repo.Should().Be("kibana-team");
	}

	[Fact]
	public void TryGetGitHubRepo_ShortForm_ParsesOwnerRepo()
	{
		var ok = ChangelogTextUtilities.TryGetGitHubRepo(
			"elastic/security-team#789",
			"elastic",
			"elasticsearch",
			out var owner,
			out var repo);

		ok.Should().BeTrue();
		owner.Should().Be("elastic");
		repo.Should().Be("security-team");
	}

	[Fact]
	public void TryGetGitHubRepo_BareNumber_UsesDefaults()
	{
		var ok = ChangelogTextUtilities.TryGetGitHubRepo(
			"123",
			"elastic",
			"elasticsearch+kibana",
			out var owner,
			out var repo);

		ok.Should().BeTrue();
		owner.Should().Be("elastic");
		repo.Should().Be("elasticsearch");
	}

	[Fact]
	public void FormatPrLink_Sentinel_ReturnsEmpty()
	{
		var s = ChangelogTextUtilities.FormatPrLink("# PRIVATE: https://github.com/elastic/x/pull/1", "x", hidePrivateLinks: false);
		s.Should().BeEmpty();
	}

	[Fact]
	public void TrySanitizeBundle_PrivateRepo_ReplacesWithSentinel()
	{
		var yaml =
			"""
			references:
			  elasticsearch:
			    private: false
			  kibana-team:
			    private: true
			""";

		var asm = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
		var bundle = new Elastic.Documentation.ReleaseNotes.Bundle
		{
			Entries =
			[
				new()
				{
					Title = "t",
					Prs = ["https://github.com/elastic/kibana-team/pull/1", "123"]
				}
			]
		};

		var ok = PrivateChangelogLinkSanitizer.TrySanitizeBundle(
			Collector,
			bundle,
			asm,
			"elastic",
			"elasticsearch",
			out var sanitized);

		ok.Should().BeTrue();
		sanitized.Entries[0].Prs![0].Should().StartWith("# PRIVATE:");
		sanitized.Entries[0].Prs![1].Should().Be("123");
	}

	[Fact]
	public void TrySanitizeBundle_UnknownRepo_EmitsError()
	{
		var yaml =
			"""
			references:
			  elasticsearch:
			    private: false
			""";

		var asm = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
		var bundle = new Elastic.Documentation.ReleaseNotes.Bundle
		{
			Entries = [new() { Title = "t", Prs = ["https://github.com/unknown-org/unknown-repo/pull/1"] }]
		};

		var ok = PrivateChangelogLinkSanitizer.TrySanitizeBundle(
			Collector,
			bundle,
			asm,
			"elastic",
			"elasticsearch",
			out _);

		ok.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
	}
}
