// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Backfill;

namespace Elastic.Changelog.Tests.Backfill;

/// <summary>
/// A document a reader does not fully understand must fail loudly and helpfully —
/// wrong schema version, wrong document kind, or a missing envelope header.
/// </summary>
public class BackfillVersionTests
{
	[Fact]
	public void Deserialize_NewerSchemaVersion_FailsWithActionableError()
	{
		var json = BackfillDocuments.Serialize(BackfillFixtures.Inventory())
			.Replace("\"schema_version\": 1", "\"schema_version\": 2", StringComparison.Ordinal);

		var act = () => BackfillDocuments.Deserialize<InventoryDocument>(json);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*written with schema version 2*only understands version 1*")
			.WithMessage("*Regenerate the document*");
	}

	[Fact]
	public void Deserialize_OlderSchemaVersion_AlsoFails()
	{
		var json = BackfillDocuments.Serialize(BackfillFixtures.Plan())
			.Replace("\"schema_version\": 1", "\"schema_version\": 0", StringComparison.Ordinal);

		var act = () => BackfillDocuments.Deserialize<PlanDocument>(json);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*schema version 0*");
	}

	[Fact]
	public void Deserialize_WrongDocumentKind_SaysWhatItFoundAndWhatWasRequested()
	{
		var json = BackfillDocuments.Serialize(BackfillFixtures.Plan());

		var act = () => BackfillDocuments.Deserialize<LedgerDocument>(json);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*contains a 'plan' document*'ledger' document was requested*");
	}

	[Fact]
	public void Deserialize_UnknownArtifactName_ListsTheValidKinds()
	{
		var json = BackfillDocuments.Serialize(BackfillFixtures.Inventory())
			.Replace("\"artifact\": \"inventory\"", "\"artifact\": \"census\"", StringComparison.Ordinal);

		var act = () => BackfillDocuments.Deserialize<InventoryDocument>(json);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*Unknown document kind 'census'*inventory, overrides, semantic-model, plan, provenance, ledger*");
	}

	[Fact]
	public void Deserialize_MissingArtifactField_FailsExplicitly()
	{
		var act = () => BackfillDocuments.Deserialize<InventoryDocument>(/*lang=json,strict*/ """{"schema_version":1,"payload":{"sources":[]}}""");

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*'artifact' field*missing*");
	}

	[Fact]
	public void Deserialize_MissingSchemaVersionField_FailsExplicitly()
	{
		var act = () => BackfillDocuments.Deserialize<InventoryDocument>(/*lang=json,strict*/ """{"artifact":"inventory","payload":{"sources":[]}}""");

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*missing the 'schema_version' field*");
	}

	[Fact]
	public void Deserialize_NonObjectDocument_FailsExplicitly()
	{
		var act = () => BackfillDocuments.Deserialize<InventoryDocument>("[1,2,3]");

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*top level must be a JSON object*");
	}

	[Fact]
	public void Deserialize_InvalidJson_FailsExplicitly()
	{
		var act = () => BackfillDocuments.Deserialize<InventoryDocument>("{ this is not json");

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*not valid JSON*");
	}

	[Fact]
	public void Deserialize_OversizedDocument_IsRefused()
	{
		var oversized = new string(' ', BackfillDocuments.MaxDocumentCharacters + 1);

		var act = () => BackfillDocuments.Deserialize<InventoryDocument>(oversized);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*safety limit*");
	}

	[Fact]
	public void SchemaVersions_CoverEveryFamily()
	{
		foreach (var kind in Enum.GetValues<BackfillArtifactKind>())
			BackfillSchemaVersions.Current(kind).Should().BePositive();
	}
}
