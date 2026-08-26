// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Scrubbing;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Scrubbing;

public class ChangelogContentScrubberTests
{
	private static readonly IReadOnlyList<string> AllowAll = ["elastic/elasticsearch"];

	private static ChangelogContentScrubber Scrubber(IReadOnlyList<string>? allowRepos = null) =>
		new(NullLoggerFactory.Instance, allowRepos ?? AllowAll);

	[Fact]
	public async Task ScrubAsync_MarkerEntry_LinkFieldPreserved()
	{
		// A marker is link: only — the scrubber must pass it through unchanged.
		var yaml = "link: \"12345\"\n";
		var scrubber = Scrubber();

		var result = await scrubber.ScrubAsync("changelog/elastic/elasticsearch/main/12346.yaml", yaml, CancellationToken.None);

		var entry = ReleaseNotesSerialization.DeserializeEntry(result.Content);
		entry.Link.Should().Be("12345", "link: must survive the scrub round-trip");
		entry.IsMarker.Should().BeTrue();
		result.CanonicalKey.Should().BeNull("markers are already canonical");
		result.Markers.Should().BeEmpty();
	}

	[Fact]
	public async Task ScrubAsync_NormalEntry_LinkIsNull()
	{
		var yaml =
			"title: Fix search performance\n" +
			"type: bug-fix\n" +
			"products:\n" +
			"  - product: elasticsearch\n";
		var scrubber = Scrubber();

		var result = await scrubber.ScrubAsync("changelog/elastic/elasticsearch/main/12345.yaml", yaml, CancellationToken.None);

		var entry = ReleaseNotesSerialization.DeserializeEntry(result.Content);
		entry.Link.Should().BeNull();
		entry.IsMarker.Should().BeFalse();
	}

	[Fact]
	public async Task ScrubAsync_NonCanonicalKey_ReturnsCanonicalKey()
	{
		var yaml =
			"title: Fix search performance\n" +
			"type: bug-fix\n" +
			"prs:\n" +
			"  - https://github.com/elastic/elasticsearch/pull/12345\n";
		var scrubber = Scrubber(["elastic/elasticsearch"]);

		var result = await scrubber.ScrubAsync(
			"changelog/elastic/elasticsearch/main/12345-fix.yaml", yaml, CancellationToken.None);

		result.CanonicalKey.Should().Be("changelog/elastic/elasticsearch/main/12345.yaml");
		result.Markers.Should().BeEmpty();
	}

	[Fact]
	public async Task ScrubAsync_AlreadyCanonicalKey_CanonicalKeyIsNull()
	{
		var yaml =
			"title: Fix search performance\n" +
			"type: bug-fix\n" +
			"prs:\n" +
			"  - https://github.com/elastic/elasticsearch/pull/12345\n";
		var scrubber = Scrubber(["elastic/elasticsearch"]);

		var result = await scrubber.ScrubAsync(
			"changelog/elastic/elasticsearch/main/12345.yaml", yaml, CancellationToken.None);

		result.CanonicalKey.Should().BeNull("source key is already canonical");
	}

	[Fact]
	public async Task ScrubAsync_MultiPrEntry_WritesMarkersForNonPrimaryPrs()
	{
		// prs [100, 200, 300] → primary is 100 (min), markers for 200 and 300
		var yaml =
			"title: Multi-PR feature\n" +
			"type: feature\n" +
			"prs:\n" +
			"  - https://github.com/elastic/elasticsearch/pull/300\n" +
			"  - https://github.com/elastic/elasticsearch/pull/100\n" +
			"  - https://github.com/elastic/elasticsearch/pull/200\n";
		var scrubber = Scrubber(["elastic/elasticsearch"]);

		var result = await scrubber.ScrubAsync(
			"changelog/elastic/elasticsearch/main/100.yaml", yaml, CancellationToken.None);

		result.CanonicalKey.Should().BeNull("source key already matches the min PR");
		result.Markers.Should().HaveCount(2);
		result.Markers.Should().Contain(m => m.Key == "changelog/elastic/elasticsearch/main/200.yaml");
		result.Markers.Should().Contain(m => m.Key == "changelog/elastic/elasticsearch/main/300.yaml");

		foreach (var (_, markerContent) in result.Markers)
		{
			var markerEntry = ReleaseNotesSerialization.DeserializeEntry(markerContent);
			markerEntry.Link.Should().Be("100");
			markerEntry.IsMarker.Should().BeTrue();
		}
	}

	[Fact]
	public async Task ScrubAsync_NoteFile_PassesThroughWithNoCanonicalKey()
	{
		var yaml =
			"title: Known issue with rollover\n" +
			"type: known-issue\n" +
			"products:\n" +
			"  - product: elasticsearch\n" +
			"    target: 9.2.0\n";
		var scrubber = Scrubber(["elastic/elasticsearch"]);

		var result = await scrubber.ScrubAsync(
			"changelog/elastic/elasticsearch/main/note-slow-rollover.yaml", yaml, CancellationToken.None);

		result.CanonicalKey.Should().BeNull("note-* files are already canonical and need no rename");
		result.Markers.Should().BeEmpty();
	}
}
