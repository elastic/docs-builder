// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;
using Nullean.Snapshots;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Where each mount of <c>navigation.yml</c> is published, which the cross-link resolver reads. The hand-written YAML walker that used to derive it
/// is gone; the expectations are what it produced for the same files, quirks included.
/// </summary>
public class SiteNavigationLoaderTests
{
	private const string NavigationPath = "/config/navigation.yml";

	/// <summary>The mappings, then the diagnostics: the rules of the loaded navigation and what mapping it reported.</summary>
	private static string Describe(string yaml)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile(NavigationPath, new MockFileData(yaml));
		var file = fileSystem.FileInfo.New(NavigationPath);
		var collector = new CapturingDiagnosticsCollector();

		var loaded = SiteNavigationLoader.ForAssembly(
			SiteNavigationLoader.Deserialize(yaml),
			AssemblyLoader.Deserialize("references:\n  open:\n"),
			skipPrivateRepositories: false,
			NavigationPath
		);
		foreach (var finding in loaded.Findings.Where(d => d.RuleId == "mount-has-path-prefix"))
			collector.Write(finding);
		var mappings = SiteNavigationLoader.GetTocMappings(loaded.Value, file, collector);

		var lines = mappings.Select(kv => $"{kv.Key} => {kv.Value.SourcePathPrefix}").Concat(collector.Captured.Select(Line));
		return string.Join('\n', lines);

		static string Line(Diagnostic d) => $"{d.Severity.ToString().ToLowerInvariant()} {d.Line}:{d.Column}:{d.Length} {d.Message}";
	}

	[Test]
	public void GetTocMappings_DeclaredPathPrefix_IsThePrefixTheMountIsPublishedUnder() =>
		Describe("toc:\n  - toc: repo://a\n    path_prefix: p/a\n").Should().MatchInlineSnapshot("repo://a/ => p/a");

	[Test]
	public void GetTocMappings_BareTocName_IsPublishedUnderItsName() =>
		Describe("toc:\n  - toc: get-started\n").Should().MatchInlineSnapshot("docs-content://get-started/ => get-started");

	// the mapping reader took the keys in the order written: a path_prefix before a bare toc name is overridden by the name
	[Test]
	public void GetTocMappings_PathPrefixWrittenBeforeABareTocName_TheNameWins() =>
		Describe("toc:\n  - path_prefix: other\n    toc: get-started\n")
			.Should()
			.MatchInlineSnapshot("docs-content://get-started/ => get-started");

	[Test]
	public void GetTocMappings_PathPrefixWrittenAfterABareTocName_ThePrefixWins() =>
		Describe("toc:\n  - toc: get-started\n    path_prefix: other\n")
			.Should()
			.MatchInlineSnapshot("docs-content://get-started/ => other");

	[Test]
	public void GetTocMappings_NoPathPrefix_FallsBackToTheSourcePathAndReports() =>
		Describe("toc:\n  - toc: repo://a\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			repo://a/ => a/
			error 2:5:0 Path prefix is not defined for: repo://a/, falling back to a/ which may be incorrect
			"""
			);

	// an empty path_prefix is reported like a missing one, but the mapping keeps it empty instead of falling back
	[Test]
	public void GetTocMappings_EmptyPathPrefix_StaysEmptyAndReports() =>
		Describe("toc:\n  - toc: repo://a\n    path_prefix: ''\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			repo://a/ => 
			error 2:5:0 Path prefix is not defined for: repo://a/, falling back to a/ which may be incorrect
			"""
			);

	[Test]
	public void GetTocMappings_SameSourceMountedTwice_IsListedOnceWithTheLastPrefix() =>
		Describe("toc:\n  - toc: repo://a/\n    path_prefix: pa\n  - toc: repo://a\n    path_prefix: pb\n")
			.Should()
			.MatchInlineSnapshot("repo://a/ => pb");

	[Test]
	public void GetTocMappings_SectionChildrenAndNestedChildren_AreAllMapped() =>
		Describe(
			"toc:\n  - section: S\n    children:\n      - toc: repo://c\n        path_prefix: c\n        children:\n          - toc: repo://e\n            path_prefix: e\n  - toc: repo://f\n    path_prefix: f\n"
		).Should().MatchInlineSnapshot("""
			repo://c/ => c
			repo://e/ => e
			repo://f/ => f
			""");

	[Test]
	public void ForAssembly_SkippingPrivateRepositories_MountsTheLocalDocsBuilderAfterTheRest()
	{
		var assembly = AssemblyLoader.Deserialize(
			"references:\n  docs-builder:\n  closed:\n    private: true\n",
			skipPrivateRepositories: true
		);
		var navigation = SiteNavigationLoader.Deserialize(
			"toc:\n  - toc: closed://one\n    path_prefix: one\n    children:\n      - toc: open://two\n        path_prefix: two\n"
		);

		var loaded = SiteNavigationLoader.ForAssembly(navigation, assembly, skipPrivateRepositories: true, NavigationPath);

		string
			.Join('\n', SiteNavigationConstraints.AllMounts(loaded.Value).Select(m => $"{m.Source} => {m.PathPrefix}"))
			.Should()
			.MatchInlineSnapshot(
				"""
				open://two/ => two
				docs-builder:/// => reference/docs-builder
				docs-builder://development/ => reference/docs-builder/dev
				docs-builder://development/link-validation => reference/docs-builder/dev/link-val
				"""
			);
		loaded.Findings.Should().BeEmpty();
	}

	[Test]
	public void ForAssembly_ChildrenPromotedBesideASibling_ReportTheCollisionAndKeepTheNavigation()
	{
		var assembly = AssemblyLoader.Deserialize("references:\n  closed:\n    private: true\n", skipPrivateRepositories: true);
		var navigation = SiteNavigationLoader.Deserialize(
			"toc:\n  - toc: closed://one\n    path_prefix: one\n    children:\n      - toc: open://two\n        path_prefix: same\n  - toc: other://three\n    path_prefix: same\n"
		);

		var loaded = SiteNavigationLoader.ForAssembly(navigation, assembly, skipPrivateRepositories: true, NavigationPath);

		loaded.Findings.Select(f => f.RuleId).Should().Equal("path-prefixes-unique");
		loaded.Value.TableOfContents.OfCase<SiteTableOfContentsRef>().Select(t => t.PathPrefix).Should().Equal("same", "same");
	}
}
