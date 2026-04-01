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
	public void TryGetGitHubRepo_PullUrl_InvalidNumber_ReturnsFalse()
	{
		var ok = ChangelogTextUtilities.TryGetGitHubRepo(
			"https://github.com/elastic/kibana-team/pull/not-a-number",
			"elastic",
			"elasticsearch",
			out _,
			out _);

		ok.Should().BeFalse();
	}

	[Fact]
	public void TryGetGitHubRepo_ShortForm_NonNumericFragment_ReturnsFalse()
	{
		var ok = ChangelogTextUtilities.TryGetGitHubRepo(
			"elastic/kibana-team#abc",
			"elastic",
			"elasticsearch",
			out _,
			out _);

		ok.Should().BeFalse();
	}

	[Fact]
	public void TryGetGitHubRepo_ShortForm_TooManySlashes_ReturnsFalse()
	{
		var ok = ChangelogTextUtilities.TryGetGitHubRepo(
			"a/b/c#123",
			"elastic",
			"elasticsearch",
			out _,
			out _);

		ok.Should().BeFalse();
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
			out var sanitized,
			out var changed);

		ok.Should().BeTrue();
		changed.Should().BeTrue();
		sanitized.Entries[0].Prs![0].Should().StartWith("# PRIVATE:");
		sanitized.Entries[0].Prs![1].Should().Be("123");
	}

	[Fact]
	public void TrySanitizeBundle_ReferenceKey_ElasticSlashRepo_Resolves()
	{
		var yaml =
			"""
			references:
			  elastic/kibana-team:
			    private: true
			""";

		var asm = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
		var bundle = new Bundle
		{
			Entries =
			[
				new()
				{
					Title = "t",
					Prs = ["https://github.com/elastic/kibana-team/pull/99"]
				}
			]
		};

		var ok = PrivateChangelogLinkSanitizer.TrySanitizeBundle(
			Collector,
			bundle,
			asm,
			"elastic",
			"elasticsearch",
			out var sanitized,
			out _);

		ok.Should().BeTrue();
		sanitized.Entries[0].Prs![0].Should().StartWith("# PRIVATE:");
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
			out _,
			out _);

		ok.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
	}

	[Fact]
	public void TrySanitizeBundle_EmptyReferences_NoPrIssueRefs_Succeeds()
	{
		var yaml =
			"""
			references: {}
			""";

		var asm = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
		var bundle = new Bundle
		{
			Entries =
			[
				new()
				{
					Title = "t",
					Prs = [],
					Issues = []
				}
			]
		};

		var ok = PrivateChangelogLinkSanitizer.TrySanitizeBundle(
			Collector,
			bundle,
			asm,
			"elastic",
			"elasticsearch",
			out var sanitized,
			out var changed);

		ok.Should().BeTrue();
		changed.Should().BeFalse();
		Collector.Errors.Should().Be(0);
		sanitized.Entries.Should().HaveCount(1);
	}

	[Fact]
	public void TrySanitizeBundle_EmptyReferences_ParseableRef_EmitsEmptyRegistryError()
	{
		var yaml =
			"""
			references: {}
			""";

		var asm = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
		var bundle = new Bundle
		{
			Entries = [new() { Title = "t", Prs = ["https://github.com/elastic/kibana/pull/1"] }]
		};

		var ok = PrivateChangelogLinkSanitizer.TrySanitizeBundle(
			Collector,
			bundle,
			asm,
			"elastic",
			"elasticsearch",
			out _,
			out _);

		ok.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("non-empty assembler.yml references", StringComparison.Ordinal));
	}

	[Fact]
	public void TrySanitizeBundle_AllPublicRefs_ChangesAppliedIsFalse()
	{
		var yaml =
			"""
			references:
			  elasticsearch:
			    private: false
			  kibana:
			    private: false
			""";

		var asm = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
		var bundle = new Bundle
		{
			Entries =
			[
				new()
				{
					Title = "t",
					Prs = ["https://github.com/elastic/elasticsearch/pull/100"],
					Issues = ["https://github.com/elastic/kibana/issues/200"]
				}
			]
		};

		var ok = PrivateChangelogLinkSanitizer.TrySanitizeBundle(
			Collector, bundle, asm, "elastic", "elasticsearch",
			out var sanitized, out var changed);

		ok.Should().BeTrue();
		changed.Should().BeFalse();
		sanitized.Entries[0].Prs![0].Should().Be("https://github.com/elastic/elasticsearch/pull/100");
		sanitized.Entries[0].Issues![0].Should().Be("https://github.com/elastic/kibana/issues/200");
	}

	[Fact]
	public void TrySanitizeBundle_AlreadySanitizedBundle_ChangesAppliedIsFalse()
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
		var bundle = new Bundle
		{
			Entries =
			[
				new()
				{
					Title = "t",
					Prs = ["https://github.com/elastic/elasticsearch/pull/100",
						"# PRIVATE: https://github.com/elastic/kibana-team/pull/1"]
				}
			]
		};

		var ok = PrivateChangelogLinkSanitizer.TrySanitizeBundle(
			Collector, bundle, asm, "elastic", "elasticsearch",
			out _, out var changed);

		ok.Should().BeTrue();
		changed.Should().BeFalse();
	}

	[Fact]
	public void TrySanitizeBundle_MixedPublicAndPrivateRefs_SanitizesOnlyPrivate()
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
		var bundle = new Bundle
		{
			Entries =
			[
				new()
				{
					Title = "public entry",
					Prs = ["https://github.com/elastic/elasticsearch/pull/1"],
					Issues = ["https://github.com/elastic/elasticsearch/issues/2"]
				},
				new()
				{
					Title = "mixed entry",
					Prs = ["https://github.com/elastic/elasticsearch/pull/3"],
					Issues = ["https://github.com/elastic/kibana-team/issues/4"]
				}
			]
		};

		var ok = PrivateChangelogLinkSanitizer.TrySanitizeBundle(
			Collector, bundle, asm, "elastic", "elasticsearch",
			out var sanitized, out var changed);

		ok.Should().BeTrue();
		changed.Should().BeTrue();
		sanitized.Entries[0].Prs![0].Should().Be("https://github.com/elastic/elasticsearch/pull/1");
		sanitized.Entries[0].Issues![0].Should().Be("https://github.com/elastic/elasticsearch/issues/2");
		sanitized.Entries[1].Prs![0].Should().Be("https://github.com/elastic/elasticsearch/pull/3");
		sanitized.Entries[1].Issues![0].Should().StartWith("# PRIVATE:");
	}

	[Fact]
	public void GetFirstRepoSegmentFromBundleRepo_Null_ReturnsEmpty() =>
		ChangelogTextUtilities.GetFirstRepoSegmentFromBundleRepo(null).Should().BeEmpty();

	[Fact]
	public void GetFirstRepoSegmentFromBundleRepo_Empty_ReturnsEmpty() =>
		ChangelogTextUtilities.GetFirstRepoSegmentFromBundleRepo("").Should().BeEmpty();

	[Fact]
	public void GetFirstRepoSegmentFromBundleRepo_Whitespace_ReturnsEmpty() =>
		ChangelogTextUtilities.GetFirstRepoSegmentFromBundleRepo("   ").Should().BeEmpty();

	[Fact]
	public void GetFirstRepoSegmentFromBundleRepo_SingleRepo_ReturnsSame() =>
		ChangelogTextUtilities.GetFirstRepoSegmentFromBundleRepo("elasticsearch").Should().Be("elasticsearch");

	[Fact]
	public void GetFirstRepoSegmentFromBundleRepo_MergedRepo_ReturnsFirst() =>
		ChangelogTextUtilities.GetFirstRepoSegmentFromBundleRepo("elasticsearch+kibana").Should().Be("elasticsearch");

	[Fact]
	public void GetFirstRepoSegmentFromBundleRepo_ThreeSegments_ReturnsFirst() =>
		ChangelogTextUtilities.GetFirstRepoSegmentFromBundleRepo("a+b+c").Should().Be("a");

	[Fact]
	public void FormatIssueLink_Sentinel_ReturnsEmpty()
	{
		var s = ChangelogTextUtilities.FormatIssueLink("# PRIVATE: https://github.com/elastic/x/issues/1", "x", hidePrivateLinks: false);
		s.Should().BeEmpty();
	}

	[Fact]
	public void FormatPrLinkAsciidoc_Sentinel_ReturnsEmpty()
	{
		var s = ChangelogTextUtilities.FormatPrLinkAsciidoc("# PRIVATE: https://github.com/elastic/x/pull/1", "x", hidePrivateLinks: false);
		s.Should().BeEmpty();
	}

	[Fact]
	public void FormatIssueLinkAsciidoc_Sentinel_ReturnsEmpty()
	{
		var s = ChangelogTextUtilities.FormatIssueLinkAsciidoc("# PRIVATE: https://github.com/elastic/x/issues/1", "x", hidePrivateLinks: false);
		s.Should().BeEmpty();
	}
}
