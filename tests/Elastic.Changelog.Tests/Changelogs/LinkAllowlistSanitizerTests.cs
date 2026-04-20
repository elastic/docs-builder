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
	private static readonly string[] AllowElasticsearch = ["elastic/elasticsearch"];
	private static readonly string[] AllowElasticsearchAndKibana = ["elastic/elasticsearch", "elastic/kibana"];

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
	public void TryApplyBundle_NullPrsAndIssues_PreservesNull_WhenUnchanged()
	{
		var bundle = new Bundle
		{
			Entries =
			[
				new()
				{
					Title = "t",
					Prs = null,
					Issues = null
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
		changed.Should().BeFalse();
		sanitized.Entries[0].Prs.Should().BeNull();
		sanitized.Entries[0].Issues.Should().BeNull();
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

	// --- BuildAllowReposFromAssembler ---

	[Fact]
	public void BuildAllowReposFromAssembler_SkipsPrivateAndSkipped()
	{
		var yaml =
			"""
			references:
			  elastic/elasticsearch:
			    private: false
			  elastic/kibana:
			    private: false
			  elastic/secret-team:
			    private: true
			  elastic/old-repo:
			    skip: true
			""";
		var asm = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
		var allow = LinkAllowlistSanitizer.BuildAllowReposFromAssembler(asm);

		allow.Should().Contain("elastic/elasticsearch");
		allow.Should().Contain("elastic/kibana");
		allow.Should().NotContain("elastic/secret-team");
		allow.Should().NotContain("elastic/old-repo");
	}

	[Fact]
	public void BuildAllowReposFromAssembler_DefaultsOwnerToElastic()
	{
		var yaml =
			"""
			references:
			  beats: {}
			""";
		var asm = AssemblyConfiguration.Deserialize(yaml, skipPrivateRepositories: false);
		var allow = LinkAllowlistSanitizer.BuildAllowReposFromAssembler(asm);

		allow.Should().Contain("elastic/beats");
	}

	[Fact]
	public void BuildAllowReposFromAssembler_EmptyReferences_ReturnsEmpty()
	{
		var asm = AssemblyConfiguration.Deserialize("references: {}", skipPrivateRepositories: false);
		var allow = LinkAllowlistSanitizer.BuildAllowReposFromAssembler(asm);

		allow.Should().BeEmpty();
	}

	// --- TryApplyChangelogEntry ---

	[Fact]
	public void TryApplyChangelogEntry_ScrubsPrsAndIssues()
	{
		var entry = new BundledEntry
		{
			Title = "Fix something",
			Prs = ["https://github.com/elastic/secret-repo/pull/1"],
			Issues = ["https://github.com/elastic/elasticsearch/issues/200"]
		};

		var allow = new[] { "elastic/elasticsearch" };
		var ok = LinkAllowlistSanitizer.TryApplyChangelogEntry(
			Collector, entry, allow, "elastic", "elasticsearch",
			out var sanitized, out var changed);

		ok.Should().BeTrue();
		changed.Should().BeTrue();
		sanitized.Prs![0].Should().StartWith("# PRIVATE:");
		sanitized.Issues![0].Should().Be("https://github.com/elastic/elasticsearch/issues/200");
	}

	[Fact]
	public void TryApplyChangelogEntry_ScrubsDescriptionText()
	{
		var entry = new BundledEntry
		{
			Title = "Fix something",
			Description = "Fixes elastic/secret-repo#42 which caused issues in https://github.com/elastic/elasticsearch/pull/100"
		};

		var allow = new[] { "elastic/elasticsearch" };
		var ok = LinkAllowlistSanitizer.TryApplyChangelogEntry(
			Collector, entry, allow, "elastic", "elasticsearch",
			out var sanitized, out var changed);

		ok.Should().BeTrue();
		changed.Should().BeTrue();
		sanitized.Description.Should().NotContain("secret-repo");
		sanitized.Description.Should().Contain("https://github.com/elastic/elasticsearch/pull/100");
	}

	[Fact]
	public void TryApplyChangelogEntry_ScrubsImpactAndAction()
	{
		var entry = new BundledEntry
		{
			Title = "Breaking change",
			Impact = "See elastic/private-infra#10 for details",
			Action = "Migrate using https://github.com/elastic/private-infra/pull/11"
		};

		var allow = new[] { "elastic/elasticsearch" };
		var ok = LinkAllowlistSanitizer.TryApplyChangelogEntry(
			Collector, entry, allow, "elastic", "elasticsearch",
			out var sanitized, out var changed);

		ok.Should().BeTrue();
		changed.Should().BeTrue();
		sanitized.Impact.Should().NotContain("private-infra");
		sanitized.Action.Should().NotContain("private-infra");
	}

	[Fact]
	public void TryApplyChangelogEntry_AllAllowed_NoChanges()
	{
		var entry = new BundledEntry
		{
			Title = "Good entry",
			Prs = ["https://github.com/elastic/elasticsearch/pull/1"],
			Issues = ["elastic/kibana#2"],
			Description = "Relates to elastic/kibana#2"
		};

		var allow = new[] { "elastic/elasticsearch", "elastic/kibana" };
		var ok = LinkAllowlistSanitizer.TryApplyChangelogEntry(
			Collector, entry, allow, "elastic", "elasticsearch",
			out var sanitized, out var changed);

		ok.Should().BeTrue();
		changed.Should().BeFalse();
		sanitized.Prs![0].Should().Be("https://github.com/elastic/elasticsearch/pull/1");
		sanitized.Issues![0].Should().Be("elastic/kibana#2");
		sanitized.Description.Should().Be("Relates to elastic/kibana#2");
	}

	[Fact]
	public void TryApplyChangelogEntry_NullFields_PreservesNulls()
	{
		var entry = new BundledEntry
		{
			Title = "Simple entry",
			Prs = null,
			Issues = null,
			Description = null,
			Impact = null,
			Action = null
		};

		var allow = new[] { "elastic/elasticsearch" };
		var ok = LinkAllowlistSanitizer.TryApplyChangelogEntry(
			Collector, entry, allow, "elastic", "elasticsearch",
			out var sanitized, out var changed);

		ok.Should().BeTrue();
		changed.Should().BeFalse();
		sanitized.Prs.Should().BeNull();
		sanitized.Issues.Should().BeNull();
		sanitized.Description.Should().BeNull();
	}

	// --- ScrubText ---

	[Fact]
	public void ScrubText_ReplacesPrivateGitHubUrl()
	{
		var changed = false;
		var result = LinkAllowlistSanitizer.ScrubText(
			"See https://github.com/elastic/secret-repo/pull/42 for details",
			AllowElasticsearch,
			ref changed);

		changed.Should().BeTrue();
		result.Should().NotContain("secret-repo");
		result.Should().Contain("for details");
	}

	[Fact]
	public void ScrubText_ReplacesPrivateShortForm()
	{
		var changed = false;
		var result = LinkAllowlistSanitizer.ScrubText(
			"Related to elastic/private-team#99",
			AllowElasticsearch,
			ref changed);

		changed.Should().BeTrue();
		result.Should().NotContain("private-team");
	}

	[Fact]
	public void ScrubText_PreservesAllowedReferences()
	{
		var changed = false;
		var result = LinkAllowlistSanitizer.ScrubText(
			"Fixed in https://github.com/elastic/elasticsearch/pull/100 and elastic/kibana#50",
			AllowElasticsearchAndKibana,
			ref changed);

		changed.Should().BeFalse();
		result.Should().Contain("https://github.com/elastic/elasticsearch/pull/100");
		result.Should().Contain("elastic/kibana#50");
	}

	[Fact]
	public void ScrubText_NullInput_ReturnsNull()
	{
		var changed = false;
		var result = LinkAllowlistSanitizer.ScrubText(
			null,
			AllowElasticsearch,
			ref changed);

		changed.Should().BeFalse();
		result.Should().BeNull();
	}

	[Fact]
	public void ScrubText_EmptyInput_ReturnsEmpty()
	{
		var changed = false;
		var result = LinkAllowlistSanitizer.ScrubText(
			"",
			AllowElasticsearch,
			ref changed);

		changed.Should().BeFalse();
		result.Should().BeEmpty();
	}

	[Fact]
	public void ScrubText_NoReferences_ReturnsUnchanged()
	{
		var changed = false;
		var result = LinkAllowlistSanitizer.ScrubText(
			"This is plain text with no GitHub references.",
			AllowElasticsearch,
			ref changed);

		changed.Should().BeFalse();
		result.Should().Be("This is plain text with no GitHub references.");
	}

	[Fact]
	public void ScrubText_MixedReferences_ScrubsOnlyPrivate()
	{
		var changed = false;
		var result = LinkAllowlistSanitizer.ScrubText(
			"Public elastic/elasticsearch#1 and private elastic/secret#2",
			AllowElasticsearch,
			ref changed);

		changed.Should().BeTrue();
		result.Should().Contain("elastic/elasticsearch#1");
		result.Should().NotContain("elastic/secret#2");
	}

	// --- Idempotency ---

	[Fact]
	public void TryApplyChangelogEntry_Idempotent_SecondPassNoChanges()
	{
		var entry = new BundledEntry
		{
			Title = "Fix something",
			Prs = ["https://github.com/elastic/secret-repo/pull/1"],
			Description = "See elastic/secret-repo#42"
		};

		var allow = new[] { "elastic/elasticsearch" };

		LinkAllowlistSanitizer.TryApplyChangelogEntry(
			Collector, entry, allow, "elastic", "elasticsearch",
			out var firstPass, out _);

		var ok = LinkAllowlistSanitizer.TryApplyChangelogEntry(
			Collector, firstPass, allow, "elastic", "elasticsearch",
			out var secondPass, out var secondChanged);

		ok.Should().BeTrue();
		secondChanged.Should().BeFalse();
		secondPass.Prs![0].Should().Be(firstPass.Prs![0]);
		secondPass.Description.Should().Be(firstPass.Description);
	}

	[Fact]
	public void ScrubText_Idempotent_SecondPassUnchanged()
	{
		var changed1 = false;
		var result1 = LinkAllowlistSanitizer.ScrubText(
			"See elastic/secret#1 for details",
			AllowElasticsearch,
			ref changed1);

		var changed2 = false;
		var result2 = LinkAllowlistSanitizer.ScrubText(
			result1,
			AllowElasticsearch,
			ref changed2);

		changed2.Should().BeFalse();
		result2.Should().Be(result1);
	}

	[Fact]
	public void ScrubText_IssueUrl_ReplacesPrivate()
	{
		var changed = false;
		var result = LinkAllowlistSanitizer.ScrubText(
			"Relates to https://github.com/elastic/secret-repo/issues/99",
			AllowElasticsearch,
			ref changed);

		changed.Should().BeTrue();
		result.Should().NotContain("secret-repo");
	}
}
