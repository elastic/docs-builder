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
using SharpYaml;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Where each mount of <c>navigation.yml</c> is published, which the cross-link resolver reads. The hand-written YAML walker that used to derive it
/// is gone; the expectations are what it produced for the same files, quirks included.
/// </summary>
public class SiteNavigationLoaderTests
{
	private const string NavigationPath = "/config/navigation.yml";

	/// <summary>The mappings, then the diagnostics the mapping wrote.</summary>
	private static string Describe(string yaml)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile(NavigationPath, new MockFileData(yaml));
		var file = fileSystem.FileInfo.New(NavigationPath);
		var collector = new CapturingDiagnosticsCollector();

		var mappings = SiteNavigationLoader.GetTocMappings(yaml, file, collector);

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

	// the mapping reader honoured a repo: key beside toc:, which the parser drops: it is reported and the repository is mounted under the enclosing bare toc name
	[Test]
	public void GetTocMappings_RepoBesideToc_ReportsItAndMountsTheRepositoryUnderTheEnclosingName() =>
		Describe("toc:\n  - toc: get-started\n    children:\n      - toc: repo://c\n        repo: other\n        path_prefix: c\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				docs-content://get-started/ => get-started
				other://get-started/ => get-started/other
				error 4:9:0 toc config defines 'repo' can not be combined with 'toc': repo://c
				"""
			);

	[Test]
	public void GetTocMappings_RepoBesideATopLevelToc_ReportsItAndMountsTheRepositoryUnderItsOwnName() =>
		Describe("toc:\n  - toc: repo://c\n    repo: other\n")
			.Should()
			.MatchInlineSnapshot(
				"""
			other:/// => /other
			error 2:5:0 toc config defines 'repo' can not be combined with 'toc': repo://c
			"""
			);

	// filtering private repositories leaves a children: with nothing under it behind; the mapping reader warns about it, without a position
	[Test]
	public void GetTocMappings_ChildrenWithNothingUnderIt_WarnsWithoutAPosition() =>
		Describe("toc:\n  - toc: open://a\n    path_prefix: a\n    children:\n  - section: S\n    children:\n")
			.Should()
			.MatchInlineSnapshot(
				"""
				open://a/ => a
				warning :: 'children' is not an array
				warning :: 'children' is not an array
				"""
			);

	// the mapping reader accepted entries the parser rejects: a repo with no toc is mounted by its repo, an entry with neither is skipped, an unparsable source is reported
	[Test]
	public void GetTocMappings_RepoWithoutToc_IsMountedByItsRepo() =>
		Describe("toc:\n  - repo: other\n  - path_prefix: x\n  - toc: open://a\n    path_prefix: a\n")
			.Should()
			.MatchInlineSnapshot("""
				other:/// => /other
				open://a/ => a
				""");

	[Test]
	public void GetTocMappings_UnparsableSource_IsReported() =>
		Describe("toc:\n  - toc: 'bad://[x'\n    path_prefix: x\n")
			.Should()
			.MatchInlineSnapshot("error 2:5:0 Source toc entry is not a valid uri: bad://[x/");

	[Test]
	public void Deserialize_RepoWithoutToc_StillThrows() =>
		FluentActions
			.Invoking(() => SiteNavigationLoader.Deserialize("toc:\n  - repo: other\n"))
			.Should()
			.Throw<YamlException>()
			.WithMessage("toc entry has no 'toc:' key and will be ignored. Found keys: . Check for typos.");

	[Test]
	public void GetTocMappings_UnknownKeyInAnEntry_IsNotReportedByTheMapping() =>
		Describe("toc:\n  - toc: repo://a\n    path_prefx: a\n    path_prefix: p\n").Should().MatchInlineSnapshot("repo://a/ => p");

	[Test]
	public void ValidatePathPrefixes_TwoRepositoriesUnderOnePath_WritesOnlyTheCollision()
	{
		var fileSystem = new MockFileSystem();
		var file = fileSystem.FileInfo.New(NavigationPath);
		var collector = new CapturingDiagnosticsCollector();
		var navigation = SiteNavigationLoader.Deserialize(
			"toc:\n  - toc: repoa://a\n    path_prefix: s\n  - toc: repob://b\n  - toc: repoc://c\n    path_prefix: s/\n"
		);

		var valid = SiteNavigationLoader.ValidatePathPrefixes(collector, navigation, file);

		valid.Should().BeFalse();
		collector.Captured.Should().ContainSingle().Which.Message.Should().StartWith("Duplicate path prefix: ");
	}
}
