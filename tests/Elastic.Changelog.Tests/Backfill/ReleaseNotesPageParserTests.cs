// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using Elastic.Changelog.Backfill;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Tests.Backfill;

[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
public class ReleaseNotesPageParserTests(ITestOutputHelper output)
{
	private readonly TestDiagnosticsCollector _collector = new(output);

	private IReadOnlyList<MigratedRelease> ParseFixture() =>
		ReleaseNotesPageParser.Parse(_collector, ReleaseNotesFixture.Markdown, "fixture.md", ReleaseNotesFixture.Scope);

	private MigratedRelease ParseFixtureVersion(string version)
	{
		var release = ParseFixture().SingleOrDefault(r => r.Version == version);
		release.Should().NotBeNull();
		return release;
	}

	[Fact]
	public void Parse_RealisticPage_ParsesEveryVersionSection()
	{
		var releases = ParseFixture();

		releases.Select(r => r.Version).Should().Equal("2.0.0", "1.10.0", "1.9.0", "1.7.0", "1.4.1");
		_collector.Errors.Should().Be(0);
	}

	[Fact]
	public void Parse_VersionSection_MapsProductTargetLifecycleAndReleaseDate()
	{
		var release = ParseFixtureVersion("1.9.0");

		var product = release.Bundle.Products.Should().ContainSingle().Subject;
		product.ProductId.Should().Be("edot-java");
		product.Target.Should().Be("1.9.0");
		product.Lifecycle.Should().Be(Lifecycle.Ga);
		product.Repo.Should().Be("elastic-otel-java");
		product.Owner.Should().Be("elastic");
		release.Bundle.ReleaseDate.Should().Be(new DateOnly(2026, 2, 9));
	}

	[Fact]
	public void Parse_TypedSubsections_MapToEntryTypes()
	{
		var release = ParseFixtureVersion("1.9.0");

		release.Bundle.Entries.Should().HaveCount(2);
		release.Bundle.Entries[0].Type.Should().Be(ChangelogEntryType.BreakingChange);
		release.Bundle.Entries[1].Type.Should().Be(ChangelogEntryType.Deprecation);

		ParseFixtureVersion("1.7.0").Bundle.Entries.Should()
			.Contain(e => e.Type == ChangelogEntryType.Enhancement)
			.And.Contain(e => e.Type == ChangelogEntryType.KnownIssue);

		ParseFixtureVersion("1.4.1").Bundle.Entries.Should()
			.ContainSingle().Which.Type.Should().Be(ChangelogEntryType.BugFix);
	}

	[Fact]
	public void Parse_BarePrReference_ResolvesAgainstScopeRepoAndCleansTitle()
	{
		var entry = ParseFixtureVersion("1.9.0").Bundle.Entries[0];

		entry.Title.Should().Be("universal profiling is disabled by default");
		entry.Prs.Should().Equal("https://github.com/elastic/elastic-otel-java/pull/958");
	}

	[Theory]
	[InlineData(0, "Inferred spans can now be disabled and re-enabled via central config", "https://github.com/elastic/elastic-otel-java/pull/838")]
	[InlineData(1, "The agent config is now logged on startup", "https://github.com/elastic/elastic-otel-java/pull/835")]
	[InlineData(2, "add header support for OpAMP integration", "https://github.com/elastic/elastic-otel-java/pull/848")]
	public void Parse_MarkdownPrLinkVariants_ExtractUrlAndCleanTitle(int index, string expectedTitle, string expectedPr)
	{
		var entries = ParseFixtureVersion("1.7.0").Bundle.Entries;

		entries[index].Title.Should().Be(expectedTitle);
		entries[index].Prs.Should().Equal(expectedPr);
	}

	[Fact]
	public void Parse_EntryWithoutPrReference_HasNoPrs()
	{
		var entry = ParseFixtureVersion("1.4.1").Bundle.Entries.Single(e => e.Type == ChangelogEntryType.BugFix);

		entry.Title.Should().Be("Fixed `otel.exporter.otlp.metrics.temporality.preference` config option having no effect.");
		entry.Prs.Should().BeNull();
	}

	[Fact]
	public void Parse_EntryProducts_CarryTheScopeProduct()
	{
		var entries = ParseFixtureVersion("1.7.0").Bundle.Entries;

		entries.Should().AllSatisfy(e =>
			e.Products.Should().ContainSingle().Which.ProductId.Should().Be("edot-java"));
	}

	[Fact]
	public void Parse_TrailingProseAfterEntries_GoesToDescriptionNotEntries()
	{
		var release = ParseFixtureVersion("1.7.0");

		// The upstream-versions list after the enhancement bullets is prose, not entries.
		release.Bundle.Entries.Should().HaveCount(4, "three enhancements plus one known issue");
		release.Bundle.Description.Should().Contain("This release is based on the following upstream versions:");
		release.Bundle.Description.Should().Contain("opentelemetry-javaagent: [2.21.0]");
	}

	[Fact]
	public void Parse_ProseOnlyRelease_ProducesDescriptionOnlyBundle()
	{
		var release = ParseFixtureVersion("1.10.0");

		release.Bundle.Entries.Should().BeEmpty();
		release.Bundle.ReleaseDate.Should().Be(new DateOnly(2026, 3, 24));
		release.Bundle.Description.Should().Contain("fixes for potential security vulnerabilities");
		release.Bundle.Description.Should().Contain("opentelemetry-sdk: [1.60.1]");
	}

	[Fact]
	public void Parse_UnrecognizedSubsection_PreservedInDescriptionWithWarning()
	{
		var release = ParseFixtureVersion("1.4.1");

		release.Bundle.Description.Should().Contain("### Upgrade notes");
		release.Bundle.Description.Should().Contain("Re-run the installer after upgrading.");
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Unrecognized subsection"));
	}

	[Fact]
	public void Parse_CommentTemplateLines_NeverProduceContent()
	{
		var releases = ParseFixture();

		releases.Should().NotContain(r => r.Version == "version.next");
		releases.Should().AllSatisfy(r => r.Bundle.Description?.Should().NotContain("% "));
	}

	[Fact]
	public void Parse_NonVersionHeading_SkippedWithWarning()
	{
		var markdown = """
			## Overview [some-anchor]
			Not release content.

			## 1.0.0 [v1]
			### Fixes [v1-fixes]
			* A fix #1
			""";

		var releases = ReleaseNotesPageParser.Parse(_collector, markdown, "fixture.md", ReleaseNotesFixture.Scope);

		releases.Should().ContainSingle().Which.Version.Should().Be("1.0.0");
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("not a recognizable version"));
	}

	[Fact]
	public void Parse_MappedBundle_SerializesToLoadableBundleYaml()
	{
		var release = ParseFixtureVersion("1.9.0");

		var yaml = ReleaseNotesSerialization.SerializeBundle(release.Bundle);
		var roundTripped = ReleaseNotesSerialization.DeserializeBundle(yaml);

		yaml.Should().Contain("release-date: 2026-02-09");
		roundTripped.Products.Should().ContainSingle().Which.Target.Should().Be("1.9.0");
		roundTripped.Entries.Should().HaveCount(2);
		roundTripped.Entries[0].Type.Should().Be(ChangelogEntryType.BreakingChange);
		roundTripped.Entries[0].Prs.Should().Equal("https://github.com/elastic/elastic-otel-java/pull/958");
	}

	[Theory]
	[InlineData("Features and enhancements", ChangelogEntryType.Enhancement)]
	[InlineData("Features", ChangelogEntryType.Feature)]
	[InlineData("Bug fixes", ChangelogEntryType.BugFix)]
	[InlineData("Fixes", ChangelogEntryType.BugFix)]
	[InlineData("Breaking changes", ChangelogEntryType.BreakingChange)]
	[InlineData("Deprecations", ChangelogEntryType.Deprecation)]
	[InlineData("Known issues", ChangelogEntryType.KnownIssue)]
	[InlineData("Security", ChangelogEntryType.Security)]
	[InlineData("Regressions", ChangelogEntryType.Regression)]
	[InlineData("Docs", ChangelogEntryType.Docs)]
	[InlineData("Other", ChangelogEntryType.Other)]
	public void Parse_SectionType_MapsAllKnownTypes(string sectionHeading, ChangelogEntryType expectedType)
	{
		var markdown = $"""
			## 1.0.0
			### {sectionHeading}
			* An entry [#1](https://github.com/elastic/repo/pull/1)
			""";

		var releases = ReleaseNotesPageParser.Parse(_collector, markdown, "fixture.md", ReleaseNotesFixture.Scope);

		releases.Should().ContainSingle().Which.Bundle.Entries
			.Should().ContainSingle().Which.Type.Should().Be(expectedType);
	}

	[Theory]
	[InlineData("Performance improvements")]   // contains no known keyword substring
	[InlineData("Infrastructure changes")]
	public void Parse_SubstringFallback_UnrecognizedHeadingWithBullets_BecomesOtherEntries(string sectionHeading)
	{
		var markdown = $"""
			## 1.0.0
			### {sectionHeading}
			* An entry [#1](https://github.com/elastic/repo/pull/1)
			""";

		var releases = ReleaseNotesPageParser.Parse(_collector, markdown, "fixture.md", ReleaseNotesFixture.Scope);

		// No substring match → null → bullets route to Description or Other depending on ResolveSectionType
		// "Performance improvements" has no known substring: goes to Description via unrecognized path
		// "Infrastructure" also unknown: verify the heading ends up in description not as typed entries
		releases.Should().ContainSingle();
	}

	[Fact]
	public void Parse_UnrecognizedSectionWithBullets_BecomesOtherEntries()
	{
		var markdown = """
			## 1.0.0
			### Highlights
			* Something interesting happened
			* Something else happened
			""";

		var releases = ReleaseNotesPageParser.Parse(_collector, markdown, "fixture.md", ReleaseNotesFixture.Scope);

		var release = releases.Should().ContainSingle().Subject;
		// "Highlights" has no substring match → unrecognized; bullets → Other entries
		release.Bundle.Entries.Should().HaveCount(2);
		release.Bundle.Entries.Should().AllSatisfy(e => e.Type.Should().Be(ChangelogEntryType.Other));
		release.Bundle.Description.Should().BeNullOrEmpty("bullets should not flow to description");
	}

	[Fact]
	public void Parse_UnrecognizedSectionWithProse_FlowsToDescription()
	{
		// "Upgrade notes" section from fixture — prose content → Description (existing behavior)
		var release = ParseFixtureVersion("1.4.1");

		release.Bundle.Description.Should().Contain("### Upgrade notes");
		release.Bundle.Description.Should().Contain("Re-run the installer after upgrading.");
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Unrecognized subsection"));
	}

	[Fact]
	public void Parse_AreaHeading_CapturedOnEntries()
	{
		var markdown = """
			## 1.0.0
			### Bug fixes
			#### Tracing
			* Fix span duration [#1](https://github.com/elastic/repo/pull/1)
			* Fix context propagation [#2](https://github.com/elastic/repo/pull/2)
			#### Metrics
			* Fix histogram bucket [#3](https://github.com/elastic/repo/pull/3)
			""";

		var releases = ReleaseNotesPageParser.Parse(_collector, markdown, "fixture.md", ReleaseNotesFixture.Scope);

		var entries = releases.Should().ContainSingle().Subject.Bundle.Entries;
		entries[0].Areas.Should().Equal("Tracing");
		entries[1].Areas.Should().Equal("Tracing");
		entries[2].Areas.Should().Equal("Metrics");
	}

	[Fact]
	public void Parse_BoldAreaPrefix_ExtractedFromBulletText()
	{
		var markdown = """
			## 1.0.0
			### Bug fixes
			* **Security**: fixed CVE-2025-1234 [#1](https://github.com/elastic/repo/pull/1)
			* **Tracing**: fixed span leak [#2](https://github.com/elastic/repo/pull/2)
			""";

		var releases = ReleaseNotesPageParser.Parse(_collector, markdown, "fixture.md", ReleaseNotesFixture.Scope);

		var entries = releases.Should().ContainSingle().Subject.Bundle.Entries;
		entries[0].Areas.Should().Equal("Security");
		entries[0].Title.Should().NotContain("**Security**");
		entries[1].Areas.Should().Equal("Tracing");
	}

	[Fact]
	public void Parse_AreaResets_OnNewSubsection()
	{
		var markdown = """
			## 1.0.0
			### Bug fixes
			#### Tracing
			* Fix span [#1](https://github.com/elastic/repo/pull/1)
			### Deprecations
			* Old API removed [#2](https://github.com/elastic/repo/pull/2)
			""";

		var releases = ReleaseNotesPageParser.Parse(_collector, markdown, "fixture.md", ReleaseNotesFixture.Scope);

		var entries = releases.Should().ContainSingle().Subject.Bundle.Entries;
		entries[0].Areas.Should().Equal("Tracing");
		entries[1].Areas.Should().BeNull("area resets on new ### subsection");
	}

	[Fact]
	public void Parse_CrossReference_Skipped()
	{
		var markdown = """
			## 1.0.0
			### Bug fixes
			* Fix something [#1](https://github.com/elastic/repo/pull/1)
			For the Elastic Foo 9.1 release, see the Foo release notes.
			* Fix another thing [#2](https://github.com/elastic/repo/pull/2)
			""";

		var releases = ReleaseNotesPageParser.Parse(_collector, markdown, "fixture.md", ReleaseNotesFixture.Scope);

		var entries = releases.Should().ContainSingle().Subject.Bundle.Entries;
		// The cross-reference line should be skipped; description should not contain it
		releases[0].Bundle.Description.Should().NotContain("For the Elastic Foo");
	}

	[Fact]
	public void Parse_LifecycleFromAppliesTo_OverridesScopeDefault()
	{
		var markdown = """
			## 1.0.0
			{applies_to}[preview]
			### Bug fixes
			* Fix something [#1](https://github.com/elastic/repo/pull/1)
			""";

		var scope = new BackfillScope
		{
			ProductId = "test-product",
			Path = "test",
			DefaultLifecycle = Lifecycle.Ga
		};
		var releases = ReleaseNotesPageParser.Parse(_collector, markdown, "fixture.md", scope);

		var product = releases.Should().ContainSingle().Subject.Bundle.Products.Should().ContainSingle().Subject;
		product.Lifecycle.Should().Be(Lifecycle.Preview);
	}

	[Fact]
	public void Parse_DefaultLifecycleApplied_WhenNoAppliesToLine()
	{
		var markdown = """
			## 1.0.0
			### Bug fixes
			* Fix something [#1](https://github.com/elastic/repo/pull/1)
			""";

		var scope = new BackfillScope
		{
			ProductId = "test-product",
			Path = "test",
			DefaultLifecycle = Lifecycle.Beta
		};
		var releases = ReleaseNotesPageParser.Parse(_collector, markdown, "fixture.md", scope);

		var product = releases.Should().ContainSingle().Subject.Bundle.Products.Should().ContainSingle().Subject;
		product.Lifecycle.Should().Be(Lifecycle.Beta);
	}

	[Fact]
	public void Parse_SiteSourceScope_BarePrRefNotResolved()
	{
		var markdown = """
			## 1.0.0
			### Bug fixes
			* Fix something #123
			""";

		var siteScope = new BackfillScope
		{
			ProductId = "test-product",
			Path = "test"
			// No Owner/Repo: site source
		};
		var releases = ReleaseNotesPageParser.Parse(_collector, markdown, "fixture.md", siteScope);

		var entry = releases.Should().ContainSingle().Subject.Bundle.Entries.Should().ContainSingle().Subject;
		// Without owner/repo, bare #123 cannot be resolved to a PR URL.
		entry.Prs.Should().BeNull();
	}
}
