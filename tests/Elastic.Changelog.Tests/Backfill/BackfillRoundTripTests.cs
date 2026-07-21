// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Changelog.Backfill;

namespace Elastic.Changelog.Tests.Backfill;

/// <summary>
/// Every document family must survive serialize → deserialize unchanged, and the
/// serialized form must carry the envelope header (artifact name + schema version).
/// </summary>
public class BackfillRoundTripTests
{
	[Fact]
	public void Serialize_InventoryDocument_RoundTripsUnchanged() =>
		AssertRoundTrip(BackfillFixtures.Inventory(), "inventory");

	[Fact]
	public void Serialize_OverridesDocument_RoundTripsUnchanged() =>
		AssertRoundTrip(BackfillFixtures.Overrides(), "overrides");

	[Fact]
	public void Serialize_SemanticModelDocument_RoundTripsUnchanged() =>
		AssertRoundTrip(BackfillFixtures.SemanticModel(), "semantic-model");

	[Fact]
	public void Serialize_PlanDocument_RoundTripsUnchanged() =>
		AssertRoundTrip(BackfillFixtures.Plan(), "plan");

	[Fact]
	public void Serialize_ProvenanceDocument_RoundTripsUnchanged() =>
		AssertRoundTrip(BackfillFixtures.Provenance(), "provenance");

	[Fact]
	public void Serialize_LedgerDocument_RoundTripsUnchanged() =>
		AssertRoundTrip(BackfillFixtures.Ledger(), "ledger");

	private static void AssertRoundTrip<T>(T document, string expectedArtifactName)
		where T : class, IBackfillDocument
	{
		var json = BackfillDocuments.Serialize(document);

		using (var parsed = JsonDocument.Parse(json))
		{
			parsed.RootElement.GetProperty("artifact").GetString().Should().Be(expectedArtifactName);
			parsed.RootElement.GetProperty("schema_version").GetInt32().Should().Be(BackfillSchemaVersions.Current(T.Kind));
			_ = parsed.RootElement.TryGetProperty("payload", out _).Should().BeTrue();
		}

		var roundTripped = BackfillDocuments.Deserialize<T>(json);
		roundTripped.Should().BeEquivalentTo(document);

		// The hash must also survive the trip: same content, same identity.
		BackfillDocuments.ComputeHash(roundTripped).Should().Be(BackfillDocuments.ComputeHash(document));
	}

	[Fact]
	public void Serialize_SemanticModelEnums_UseKebabCaseNames()
	{
		var json = BackfillDocuments.Serialize(BackfillFixtures.SemanticModel());

		json.Should().Contain("\"features-and-enhancements\"");
		json.Should().Contain("\"breaking-changes\"");
		json.Should().Contain("\"pull-request\"");
		json.Should().Contain("\"synthetic-file\"");
	}

	[Fact]
	public void Serialize_PlanActionKinds_UseKebabCaseNames()
	{
		var json = BackfillDocuments.Serialize(BackfillFixtures.Plan());

		json.Should().Contain("\"create-bundle\"");
		json.Should().Contain("\"create-amend\"");
		json.Should().Contain("\"skip-existing\"");
		json.Should().Contain("\"manual-review\"");
	}

	[Fact]
	public void Serialize_ReleaseDate_UsesPlainIsoDate()
	{
		var json = BackfillDocuments.Serialize(BackfillFixtures.SemanticModel());

		json.Should().Contain("\"release_date\": \"2025-04-08\"");
	}

	[Fact]
	public void Serialize_NullOptionalFields_AreOmitted()
	{
		var json = BackfillDocuments.Serialize(BackfillFixtures.Overrides());

		// The second override has no value (it is a removal); the property must be absent, not null.
		json.Should().NotContain("\"value\": null");
	}

	[Fact]
	public void ArtifactKindNames_RoundTripThroughTryParse()
	{
		foreach (var kind in Enum.GetValues<BackfillArtifactKind>())
		{
			var name = BackfillArtifactKinds.Name(kind);
			_ = BackfillArtifactKinds.TryParse(name, out var parsed).Should().BeTrue();
			parsed.Should().Be(kind);
		}
	}
}
