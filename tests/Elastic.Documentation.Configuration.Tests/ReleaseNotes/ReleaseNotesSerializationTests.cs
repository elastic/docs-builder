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

	// --- Adversarial round-trip tests 

	[Theory]
	[InlineData("Title with \"double\" quotes")]
	[InlineData("Title with 'single' quotes")]
	[InlineData("Title with: embedded colon")]
	[InlineData("Title with #leading-comment-marker")]
	[InlineData("Title with !tag-like marker")]
	[InlineData("Title with &anchor and *alias")]
	[InlineData("Title ending with backslash \\")]
	[InlineData("Title with | pipe character")]
	[InlineData("Title with > folded marker")]
	[InlineData("Title with newline\nthen colon: injected: true")]
	[InlineData("title:\nmalicious: true")]
	[InlineData("\u202E right-to-left override")]
	public void SerializeEntry_AdversarialTitle_RoundTripsWithoutInjection(string adversarialTitle)
	{
		var entry = new ChangelogEntry
		{
			Title = adversarialTitle,
			Type = ChangelogEntryType.Feature,
			Products =
			[
				new ProductReference { ProductId = "kibana", Lifecycle = Lifecycle.Ga }
			]
		};

		var yaml = ReleaseNotesSerialization.SerializeEntry(entry);
		var roundTrip = ReleaseNotesSerialization.DeserializeEntry(yaml);

		roundTrip.Title.Should().Be(adversarialTitle,
			"adversarial titles must round-trip exactly without leaking into surrounding YAML structure");
		roundTrip.Type.Should().Be(ChangelogEntryType.Feature,
			"adversarial title must not change unrelated fields");
	}

	[Fact]
	public void SerializeEntry_DescriptionWithYamlBlockMarkers_RoundTrips()
	{
		var entry = new ChangelogEntry
		{
			Title = "Plain title",
			Description = "First line\n---\nfake: document\n...\nclosing marker",
			Type = ChangelogEntryType.Feature,
			Products =
			[
				new ProductReference { ProductId = "kibana", Lifecycle = Lifecycle.Ga }
			]
		};

		var yaml = ReleaseNotesSerialization.SerializeEntry(entry);
		var roundTrip = ReleaseNotesSerialization.DeserializeEntry(yaml);

		roundTrip.Description.Should().Be("First line\n---\nfake: document\n...\nclosing marker");
		roundTrip.Title.Should().Be("Plain title");
	}

	[Fact]
	public void SerializeEntry_InjectedFieldInTitle_DoesNotPolluteOtherFields()
	{
		// A hostile title that tries to make the deserializer believe extra
		// fields exist at the entry level.
		var entry = new ChangelogEntry
		{
			Title = "Legit\nimpact: attacker-set\naction: rm -rf /",
			Type = ChangelogEntryType.Feature,
			Products =
			[
				new ProductReference { ProductId = "kibana", Lifecycle = Lifecycle.Ga }
			]
		};

		var yaml = ReleaseNotesSerialization.SerializeEntry(entry);
		var roundTrip = ReleaseNotesSerialization.DeserializeEntry(yaml);

		roundTrip.Title.Should().Be("Legit\nimpact: attacker-set\naction: rm -rf /");
		roundTrip.Impact.Should().BeNull();
		roundTrip.Action.Should().BeNull();
	}
}
