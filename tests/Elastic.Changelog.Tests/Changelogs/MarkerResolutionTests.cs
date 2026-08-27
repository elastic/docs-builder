// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Verifies that <see cref="ChangelogEntryMatcher"/> handles <c>link:</c> markers correctly:
/// markers are excluded from bundle output; the parent entry is included once;
/// missing parents and chained markers are hard errors.
/// </summary>
public class MarkerResolutionTests
{
	// language=yaml
	private const string RealEntry =
		"""
		title: Fix the thing
		type: bug-fix
		prs:
		  - "https://github.com/elastic/elasticsearch/pull/100"
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    lifecycle: ga
		""";

	private static ChangelogEntryMatcher BuildMatcher() =>
		new(new MockFileSystem(), ReleaseNotesSerialization.GetEntryDeserializer(), NullLogger.Instance);

	private static ChangelogFilterCriteria AllEntries() =>
		new() { IncludeAll = true, ProductFilters = [], PrsToMatch = [], IssuesToMatch = [] };

	private Cancel Ctx => TestContext.Current.CancellationToken;

	[Fact]
	public async Task Marker_IsExcludedFromOutput_ParentIncludedOnce()
	{
		var matcher = BuildMatcher();
		var contents = new List<(string FileName, string Content)> { ("100.yaml", RealEntry), ("200.yaml", "link: 100\n") };
		await using var collector = new DiagnosticsCollector([]);

		var result = matcher.MatchChangelogContents(collector, contents, AllEntries(), Ctx);

		result.Entries.Should().HaveCount(1, "marker must be suppressed from bundle output");
		result.Entries[0].FileName.Should().Be("100.yaml");
		collector.Errors.Should().Be(0, "no errors when parent is found");
	}

	[Fact]
	public async Task Marker_MissingParent_EmitsError()
	{
		var matcher = BuildMatcher();
		var contents = new List<(string FileName, string Content)> { ("200.yaml", "link: 100\n") };
		await using var collector = new DiagnosticsCollector([]);

		_ = matcher.MatchChangelogContents(collector, contents, AllEntries(), Ctx);

		collector.Errors.Should().BeGreaterThan(0, "a marker with no parent is a hard error");
	}

	[Fact]
	public async Task Marker_PointingAtAnotherMarker_EmitsError()
	{
		var matcher = BuildMatcher();
		var contents = new List<(string FileName, string Content)>
		{
			// 100.yaml is itself a marker → parent is a marker → depth > 1
			("100.yaml", "link: 50\n"),
			("200.yaml", "link: 100\n")
		};
		await using var collector = new DiagnosticsCollector([]);

		_ = matcher.MatchChangelogContents(collector, contents, AllEntries(), Ctx);

		collector.Errors.Should().BeGreaterThan(0, "marker chains (depth > 1) must error");
	}

	[Fact]
	public async Task TwoMarkers_SameParent_OneEntryInOutput()
	{
		var matcher = BuildMatcher();
		var contents = new List<(string FileName, string Content)>
		{
			("100.yaml", RealEntry),
			("200.yaml", "link: 100\n"),
			("300.yaml", "link: 100\n")
		};
		await using var collector = new DiagnosticsCollector([]);

		var result = matcher.MatchChangelogContents(collector, contents, AllEntries(), Ctx);

		result.Entries.Should().HaveCount(1, "both markers are suppressed; parent appears once");
		collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task NoMarkers_NormalEntries_Unaffected()
	{
		var matcher = BuildMatcher();
		var contents = new List<(string FileName, string Content)> { ("100.yaml", RealEntry) };
		await using var collector = new DiagnosticsCollector([]);

		var result = matcher.MatchChangelogContents(collector, contents, AllEntries(), Ctx);

		result.Entries.Should().HaveCount(1);
		collector.Errors.Should().Be(0);
	}
}
