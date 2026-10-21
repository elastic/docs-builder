// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

/// <summary>
/// The release-notes loader is the one way an entry or a bundle becomes a record: every path (a file, CDN content, a folder) parses, maps and checks
/// once, reports every finding in the <see cref="Result{T}"/>, and a file that does not parse is an error with no record.
/// </summary>
public class ReleaseNotesLoaderTests
{
	// language=yaml
	private const string EntryYaml =
		"""
		title: Fix the thing
		type: bug-fix
		products:
		  - product: elasticsearch
		    target: 9.3.0
		""";

	// language=yaml
	private const string BundleYaml =
		"""
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    repo: elasticsearch
		    owner: elastic
		entries:
		  - file:
		      name: 1.yaml
		      checksum: c0ffee
		    type: enhancement
		    title: Sample enhancement
		""";

	[Test]
	public void LoadEntry_StampsTheFileNameAndReadsTheEntry()
	{
		var result = ReleaseNotesLoader.LoadEntry(EntryYaml, "42-fix.yaml", "docs/changelog/42-fix.yaml");

		result.HasErrors.Should().BeFalse();
		result.Value!.FileName.Should().Be("42-fix.yaml");
		result.Value.Title.Should().Be("Fix the thing");
		result.Value.Products![0].Versions.Should().Equal("9.3.0");
	}

	[Test]
	public void LoadEntry_AFileThatDoesNotParseIsAnErrorWithNoRecord()
	{
		var result = ReleaseNotesLoader.LoadEntry("title: [unclosed", "1.yaml", "docs/changelog/1.yaml");

		result.Value.Should().BeNull();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error && d.File == "docs/changelog/1.yaml");
	}

	[Test]
	public void LoadEntry_AnEmptyFileIsAnErrorNotACrash()
	{
		var result = ReleaseNotesLoader.LoadEntry("", "1.yaml");

		result.Value.Should().BeNull();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error);
	}

	[Test]
	public void LoadEntry_ReportsEveryViolationAndStillReturnsTheRecord()
	{
		var result = ReleaseNotesLoader.LoadEntry("link: abc\ntitle: not allowed\ntype: feature\n", "7.yaml");

		result.Value.Should().NotBeNull("the caller decides whether a rule error stops it");
		result
			.Diagnostics
			.Count(d => d.RuleId == ChangelogEntryConstraints.MarkerCarriesOnlyLink.Name)
			.Should()
			.Be(2, "the title and the type are both reported");
		result.Diagnostics.Should().Contain(d => d.RuleId == ChangelogEntryConstraints.MarkerLinkIsPrNumber.Name);
	}

	[Test]
	public void LoadEntry_RunsTheReadSetOnce_AndLeavesTheAuthoringRulesToTheValidator()
	{
		// a published object that predates the authoring rules (no products, no title or type, a long title, a file name without a PR number) must still be readable without noise
		var longTitle = new string('x', 90);
		var result = ReleaseNotesLoader.LoadEntry($"title: {longTitle}\n", "no-pr-number.yaml");

		result.Diagnostics.Should().BeEmpty();
		ChangelogEntryConstraints
			.All
			.Report(result.Value!)
			.Violations
			.Should()
			.NotBeEmpty("the full set holds them to the authoring rules");
		ChangelogEntryReadConstraints.All.Report(result.Value!).Violations.Should().BeEmpty();
	}

	[Test]
	public void LoadEntry_AMarkerThatCarriesContentIsReported()
	{
		var result = ReleaseNotesLoader.LoadEntry("link: 100\ntitle: not allowed\n", "200.yaml");

		result.Diagnostics.Should().Contain(d => d.RuleId == ChangelogEntryConstraints.MarkerCarriesOnlyLink.Name);
	}

	[Test]
	public void LoadBundle_ParsesAndChecksOnce()
	{
		var result = ReleaseNotesLoader.LoadBundle(BundleYaml, "9.3.0.yaml");

		result.HasErrors.Should().BeFalse();
		result.Diagnostics.Should().BeEmpty();
		result.Value!.Products.Should().ContainSingle().Which.ProductId.Should().Be("elasticsearch");
	}

	[Test]
	public void LoadBundle_ReportsAMalformedReleaseDateOnce()
	{
		var result = ReleaseNotesLoader.LoadBundle("release-date: soon\n" + BundleYaml, "9.3.0.yaml");

		result.Value.Should().NotBeNull();
		result.Diagnostics.Count(d => d.RuleId == BundleConstraints.BundleReleaseDateParses.Name).Should().Be(1);
	}

	[Test]
	public void LoadBundle_AFileThatDoesNotParseIsAnErrorWithNoRecord()
	{
		var result = ReleaseNotesLoader.LoadBundle("products: [unclosed", "9.3.0.yaml");

		result.Value.Should().BeNull();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error && d.File == "9.3.0.yaml");
	}

	[Test]
	public void LoadBundle_StageRulesAreLeftToTheLoaderAndTheRenderer()
	{
		// an entry without inline content is the bundle loader's own skip-with-warning, and file uniqueness is the render CLI's: neither is a read-time rule
		const string yaml =
			"products:\n  - product: elasticsearch\n    target: 9.3.0\nentries:\n  - file:\n      name: 1.yaml\n      checksum: a\n  - file:\n      name: 1.yaml\n      checksum: a\n";

		var result = ReleaseNotesLoader.LoadBundle(yaml, "9.3.0.yaml");

		result.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public void BundleLoader_FromContentChecksEveryBundleAndReportsAllFindings()
	{
		var loader = new BundleLoader(new MockFileSystem());
		// two targets for one product and a malformed date (bundle rules), plus a file that does not parse
		const string oddBundle =
			"release-date: soon\nproducts:\n  - product: elasticsearch\n    target: 9.3.0\n  - product: elasticsearch\n    target: 9.4.0\nentries: []\n";
		var result = loader.LoadBundlesFromContent([("odd-name.yaml", oddBundle), ("broken.yaml", "products: [unclosed")]);

		result.Value!.Should().ContainSingle("the bundle that parsed is loaded");
		var ruleIds = result.Diagnostics.Select(d => d.RuleId).ToList();
		ruleIds.Should().Contain(BundleConstraints.BundleProductSingleTarget.Name);
		ruleIds.Should().Contain(BundleConstraints.BundleReleaseDateParses.Name);
		ruleIds.Should().NotContain(
			LoadedBundleConstraints.BundleFilenameConvention.Name,
			"a CDN object is named by the registry, not by the generator"
		);
		result
			.Diagnostics
			.Should()
			.ContainSingle(d => d.RuleId == null && d.Severity == Severity.Error && d.Message.Contains("'broken.yaml'"));
	}

	[Test]
	public void BundleLoader_FromAFolderChecksTheLoadedBundlesWithoutBeingAsked()
	{
		var fs = new MockFileSystem();
		fs.AddFile("/bundles/odd-name.yaml", new MockFileData(BundleYaml));
		fs.AddFile("/bundles/broken.yaml", new MockFileData("entries: [unclosed"));

		var result = new BundleLoader(fs).LoadBundles("/bundles");

		result.Value!.Should().ContainSingle();
		result.Diagnostics.Should().Contain(d => d.RuleId == LoadedBundleConstraints.BundleFilenameConvention.Name);
		result.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("'broken.yaml'"));
	}

	[Test]
	public void BundleLoader_AnEntryWithoutInlineContentIsSkippedWithAnErrorDiagnostic()
	{
		var fs = new MockFileSystem();
		fs.AddFile(
			"/bundles/9.3.0.yaml",
			new MockFileData(
				"products:\n  - product: elasticsearch\n    target: 9.3.0\nentries:\n  - file:\n      name: 1.yaml\n      checksum: a\n"
			)
		);

		var result = new BundleLoader(fs).LoadBundles("/bundles");

		result.Value![0].Entries.Should().BeEmpty();
		result.Diagnostics.Should().ContainSingle(d => d.Severity == Severity.Error && d.Message.Contains("has no inline content"));
	}

	[Test]
	public void Serialize_RoundTripsThroughTheLoader()
	{
		var entry = ReleaseNotesLoader.LoadEntry(EntryYaml, "1.yaml").Value!;
		var again = ReleaseNotesLoader.LoadEntry(ReleaseNotesLoader.Serialize(entry), "1.yaml").Value!;
		again.Title.Should().Be(entry.Title);

		var bundle = ReleaseNotesLoader.LoadBundle(BundleYaml, "9.3.0.yaml").Value!;
		ReleaseNotesLoader.LoadBundle(ReleaseNotesLoader.Serialize(bundle), "9.3.0.yaml").Value!.Entries.Should().HaveCount(1);
	}
}
