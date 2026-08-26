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

		var entry = ReleaseNotesSerialization.DeserializeEntry(result);
		entry.Link.Should().Be("12345", "link: must survive the scrub round-trip");
		entry.IsMarker.Should().BeTrue();
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

		var entry = ReleaseNotesSerialization.DeserializeEntry(result);
		entry.Link.Should().BeNull();
		entry.IsMarker.Should().BeFalse();
	}
}
