// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Tests.Changelogs;

public class LinkAllowlistSanitizerTests(ITestOutputHelper output) : ChangelogTestBase(output)
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
	public void TryApplyBundle_AllowedRepo_KeepsUrl()
	{
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

		var allow = new[] { "elastic/elasticsearch", "elastic/kibana" };
		var ok = LinkAllowlistSanitizer.TryApplyBundle(
			Collector,
			bundle,
			allow,
			"elastic",
			"elasticsearch",
			out var sanitized,
			out var changed);

		ok.Should().BeTrue();
		changed.Should().BeFalse();
		sanitized.Entries[0].Prs![0].Should().Be("https://github.com/elastic/elasticsearch/pull/100");
		sanitized.Entries[0].Issues![0].Should().Be("https://github.com/elastic/kibana/issues/200");
		Collector.Errors.Should().Be(0);
	}

	[Fact]
	public void TryApplyBundle_NotAllowed_ReplacesWithSentinel()
	{
		var bundle = new Bundle
		{
			Entries =
			[
				new()
				{
					Title = "t",
					Prs = ["https://github.com/elastic/secret-repo/pull/1"]
				}
			]
		};

		var allow = new[] { "elastic/elasticsearch" };
		var ok = LinkAllowlistSanitizer.TryApplyBundle(
			Collector,
			bundle,
			allow,
			"elastic",
			"elasticsearch",
			out var sanitized,
			out var changed);

		ok.Should().BeTrue();
		changed.Should().BeTrue();
		sanitized.Entries[0].Prs![0].Should().StartWith("# PRIVATE:");
		Collector.Warnings.Should().BeGreaterThan(0);
	}

	[Fact]
	public void TryApplyBundle_EmptyAllowlist_StripsAll()
	{
		var bundle = new Bundle
		{
			Entries = [new() { Title = "t", Prs = ["https://github.com/elastic/elasticsearch/pull/1"] }]
		};

		var ok = LinkAllowlistSanitizer.TryApplyBundle(
			Collector,
			bundle,
			[],
			"elastic",
			"elasticsearch",
			out var sanitized,
			out var changed);

		ok.Should().BeTrue();
		changed.Should().BeTrue();
		sanitized.Entries[0].Prs![0].Should().StartWith("# PRIVATE:");
	}

	[Fact]
	public void TryApplyBundle_UnparseableRef_EmitsError()
	{
		var bundle = new Bundle
		{
			Entries = [new() { Title = "t", Prs = ["not-a-valid-ref"] }]
		};

		var ok = LinkAllowlistSanitizer.TryApplyBundle(
			Collector,
			bundle,
			["elastic/elasticsearch"],
			"elastic",
			"elasticsearch",
			out _,
			out _);

		ok.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
	}

	[Fact]
	public void TryApplyBundle_SentinelAllowed_RestoresPlainRef()
	{
		var bundle = new Bundle
		{
			Entries =
			[
				new()
				{
					Title = "t",
					Prs = ["# PRIVATE: https://github.com/elastic/elasticsearch/pull/1"]
				}
			]
		};

		var ok = LinkAllowlistSanitizer.TryApplyBundle(
			Collector,
			bundle,
			["elastic/elasticsearch"],
			"elastic",
			"elasticsearch",
			out var sanitized,
			out var changed);

		ok.Should().BeTrue();
		changed.Should().BeTrue();
		sanitized.Entries[0].Prs![0].Should().Be("https://github.com/elastic/elasticsearch/pull/1");
	}

	[Fact]
	public void TryApplyBundle_SentinelNotAllowed_KeepsSentinel()
	{
		var bundle = new Bundle
		{
			Entries =
			[
				new()
				{
					Title = "t",
					Prs = ["# PRIVATE: https://github.com/elastic/other/pull/1"]
				}
			]
		};

		var ok = LinkAllowlistSanitizer.TryApplyBundle(
			Collector,
			bundle,
			["elastic/elasticsearch"],
			"elastic",
			"elasticsearch",
			out var sanitized,
			out var changed);

		ok.Should().BeTrue();
		changed.Should().BeFalse();
		sanitized.Entries[0].Prs![0].Should().Be("# PRIVATE: https://github.com/elastic/other/pull/1");
	}

	[Fact]
	public void EmitAssemblerDiagnostics_MissingRepo_EmitsWarning()
	{
		var asm = AssemblyConfiguration.Deserialize("references: {}", skipPrivateRepositories: false);
		LinkAllowlistSanitizer.EmitAssemblerDiagnostics(Collector, ["elastic/foo"], asm);
		Collector.Warnings.Should().BeGreaterThan(0);
	}

	[Fact]
	public void EmitAssemblerDiagnostics_PrivateRepo_EmitsWarning()
	{
		var yaml =
			"""
			references:
			  elastic/foo:
			    private: true
			""";
		var asm = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
		LinkAllowlistSanitizer.EmitAssemblerDiagnostics(Collector, ["elastic/foo"], asm);
		Collector.Warnings.Should().BeGreaterThan(0);
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
