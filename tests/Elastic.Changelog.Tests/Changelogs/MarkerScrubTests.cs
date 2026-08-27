// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Scrubbing;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Verifies that <see cref="ChangelogContentScrubber"/> handles <c>link:</c> markers correctly:
/// a bare marker is re-serialized with the link value preserved; a marker with content fields throws.
/// Re-serialization (rather than pass-through) strips private-authored fields such as
/// <c>source-redirect: true</c> so forged markers cannot impersonate scrubber-written source pointers.
/// </summary>
public class MarkerScrubTests
{
	private readonly IChangelogContentScrubber _scrubber =
		new ChangelogContentScrubber(NullLoggerFactory.Instance, ["elastic/elasticsearch"]);

	private Cancel Ctx => TestContext.Current.CancellationToken;

	[Fact]
	public async Task Marker_OnlyLink_PreservesLinkValue()
	{
		const string key = "changelog/elastic/elasticsearch/main/200.yaml";
		const string content = "link: 100\n";

		var result = await _scrubber.ScrubAsync(key, content, Ctx);

		var entry = ReleaseNotesSerialization.DeserializeEntry(result.Content);
		entry.Link.Should().Be("100", "link: value must survive the scrub");
		entry.SourceRedirect.Should().BeFalse("source-redirect must not appear in scrubbed markers");
		result.IsMarker.Should().BeTrue();
	}

	[Fact]
	public async Task Marker_OnlyLink_NoTrailingNewline_PreservesLinkValue()
	{
		const string key = "changelog/elastic/elasticsearch/main/200.yaml";
		const string content = "link: 100";

		var result = await _scrubber.ScrubAsync(key, content, Ctx);

		var entry = ReleaseNotesSerialization.DeserializeEntry(result.Content);
		entry.Link.Should().Be("100", "link: value must survive the scrub even without a trailing newline");
		result.IsMarker.Should().BeTrue();
	}

	[Fact]
	public async Task Marker_WithTitle_ThrowsInvalidOperation()
	{
		const string key = "changelog/elastic/elasticsearch/main/200.yaml";
		const string content = """
			link: 100
			title: This is wrong
			""";

		var act = async () => await _scrubber.ScrubAsync(key, content, Ctx);

		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*link:*content fields*");
	}

	[Fact]
	public async Task Marker_WithType_ThrowsInvalidOperation()
	{
		const string key = "changelog/elastic/elasticsearch/main/200.yaml";
		const string content = """
			link: 100
			type: bug-fix
			""";

		var act = async () => await _scrubber.ScrubAsync(key, content, Ctx);

		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Fact]
	public async Task Marker_WithPrs_ThrowsInvalidOperation()
	{
		const string key = "changelog/elastic/elasticsearch/main/200.yaml";
		const string content = """
			link: 100
			prs:
			  - "https://github.com/elastic/elasticsearch/pull/100"
			""";

		var act = async () => await _scrubber.ScrubAsync(key, content, Ctx);

		await act.Should().ThrowAsync<InvalidOperationException>();
	}
}
