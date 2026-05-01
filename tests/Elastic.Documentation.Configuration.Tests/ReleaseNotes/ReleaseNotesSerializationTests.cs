// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

public class ReleaseNotesSerializationTests
{
	[Fact]
	public void SerializeEntry_TitleStartingWithDash_EmitsDoubleQuotedTitleAndRoundTrips()
	{
		var entry = new ChangelogEntry
		{
			Title = "- Manual leading dash",
			Type = ChangelogEntryType.Feature,
			Products =
			[
				new ProductReference { ProductId = "kibana", Lifecycle = Lifecycle.Ga }
			]
		};

		var yaml = ReleaseNotesSerialization.SerializeEntry(entry);

		(yaml.Contains("title: \"- Manual leading dash\"", StringComparison.Ordinal) ||
			yaml.Contains("title: '- Manual leading dash'", StringComparison.Ordinal))
			.Should().BeTrue("title must be a quoted YAML scalar so '-' is not parsed as a list marker");

		var roundTrip = ReleaseNotesSerialization.DeserializeEntry(yaml);
		roundTrip.Title.Should().Be("- Manual leading dash");
	}

	[Fact]
	public void SerializeEntry_PlainTitle_DoesNotForceDoubleQuotes()
	{
		var entry = new ChangelogEntry
		{
			Title = "Enable numerical id service",
			Type = ChangelogEntryType.Feature,
			Products =
			[
				new ProductReference { ProductId = "kibana", Lifecycle = Lifecycle.Ga }
			]
		};

		var yaml = ReleaseNotesSerialization.SerializeEntry(entry);

		yaml.Should().Contain("title: Enable numerical id service");
		yaml.Should().NotContain("title: \"Enable numerical id service\"");
	}

	[Fact]
	public void SerializeEntry_MultilineTitleStartingWithDash_RoundTrips()
	{
		var entry = new ChangelogEntry
		{
			Title = "- line1\nline2",
			Type = ChangelogEntryType.Feature,
			Products =
			[
				new ProductReference { ProductId = "kibana", Lifecycle = Lifecycle.Ga }
			]
		};

		var yaml = ReleaseNotesSerialization.SerializeEntry(entry);

		var roundTrip = ReleaseNotesSerialization.DeserializeEntry(yaml);
		roundTrip.Title.Should().Be("- line1\nline2");
	}
}
