// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using Elastic.Changelog.Migration;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Tests.Migration;

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
}
