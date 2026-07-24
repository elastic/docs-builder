// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Backfill;

namespace Elastic.Changelog.Tests.Backfill;

/// <summary>
/// The determinism guarantees: the same content always hashes the same, no matter how it
/// was formatted or assembled, and any change in meaning changes the hash.
/// </summary>
public class BackfillHashTests
{
	[Fact]
	public void ComputeHash_SameDocument_AlwaysProducesSameHash()
	{
		var first = BackfillDocuments.ComputeHash(BackfillFixtures.Plan());
		var second = BackfillDocuments.ComputeHash(BackfillFixtures.Plan());

		second.Should().Be(first);
	}

	[Fact]
	public void ComputeHash_AllFamilies_ProduceWellFormedHashes()
	{
		var hashes = new[]
		{
			BackfillDocuments.ComputeHash(BackfillFixtures.Inventory()),
			BackfillDocuments.ComputeHash(BackfillFixtures.Overrides()),
			BackfillDocuments.ComputeHash(BackfillFixtures.SemanticModel()),
			BackfillDocuments.ComputeHash(BackfillFixtures.Plan()),
			BackfillDocuments.ComputeHash(BackfillFixtures.Provenance()),
			BackfillDocuments.ComputeHash(BackfillFixtures.Ledger())
		};

		foreach (var hash in hashes)
			_ = BackfillHash.IsWellFormed(hash).Should().BeTrue($"'{hash}' should be sha256: plus 64 lower-case hex characters");

		hashes.Distinct().Should().HaveCount(hashes.Length, "different documents must not collide on the same hash");
	}

	[Fact]
	public void ComputeHash_DictionaryInsertionOrder_DoesNotChangeHash()
	{
		var source = BackfillFixtures.Inventory().Sources[0];

		var oneWay = source with
		{
			Substitutions = new Dictionary<string, string> { ["es"] = "Elasticsearch", ["kib"] = "Kibana" }
		};
		var otherWay = source with
		{
			Substitutions = new Dictionary<string, string> { ["kib"] = "Kibana", ["es"] = "Elasticsearch" }
		};

		var oneHash = BackfillDocuments.ComputeHash(new InventoryDocument { Sources = [oneWay] });
		var otherHash = BackfillDocuments.ComputeHash(new InventoryDocument { Sources = [otherWay] });

		otherHash.Should().Be(oneHash);
	}

	[Fact]
	public void ComputeHash_LedgerDictionaryInsertionOrder_DoesNotChangeHash()
	{
		var ledger = BackfillFixtures.Ledger();

		var oneWay = ledger with
		{
			CreatedObjectHashes = new Dictionary<string, string>
			{
				["bundle/es/a.yaml"] = BackfillFixtures.SampleHash,
				["bundle/es/b.yaml"] = BackfillFixtures.SampleHash
			}
		};
		var otherWay = ledger with
		{
			CreatedObjectHashes = new Dictionary<string, string>
			{
				["bundle/es/b.yaml"] = BackfillFixtures.SampleHash,
				["bundle/es/a.yaml"] = BackfillFixtures.SampleHash
			}
		};

		BackfillDocuments.ComputeHash(otherWay).Should().Be(BackfillDocuments.ComputeHash(oneWay));
	}

	[Fact]
	public void ComputeHash_FormattingOfPersistedFile_DoesNotChangeHash()
	{
		var document = BackfillFixtures.Plan();
		var indented = BackfillDocuments.Serialize(document);
		// Simulate a file that was reformatted (e.g. by an editor) without changing content.
		var compact = CanonicalJson.Canonicalize(indented);

		BackfillDocuments.ComputeHash(compact).Should().Be(BackfillDocuments.ComputeHash(indented));
		BackfillDocuments.ComputeHash(indented).Should().Be(BackfillDocuments.ComputeHash(document));
	}

	[Fact]
	public void ComputeHash_LineEndingDifferencesInsideText_DoNotChangeHash()
	{
		var release = BackfillFixtures.SemanticModel().Releases[0];

		var withUnixEndings = new SemanticModelDocument
		{
			Releases = [release with { Description = "line one\nline two" }]
		};
		var withWindowsEndings = new SemanticModelDocument
		{
			Releases = [release with { Description = "line one\r\nline two" }]
		};

		BackfillDocuments.ComputeHash(withWindowsEndings).Should().Be(BackfillDocuments.ComputeHash(withUnixEndings));
	}

	[Fact]
	public void ComputeHash_SemanticChange_ChangesHash()
	{
		var plan = BackfillFixtures.Plan();
		var changed = plan with
		{
			Actions =
			[
				.. plan.Actions.Take(plan.Actions.Count - 1),
				plan.Actions[^1] with { Reason = "A different reason." }
			]
		};

		BackfillDocuments.ComputeHash(changed).Should().NotBe(BackfillDocuments.ComputeHash(plan));
	}

	[Fact]
	public void ComputeHash_ListOrder_IsPartOfTheMeaning()
	{
		var ledger = BackfillFixtures.Ledger();
		var reversed = ledger with { Actions = [.. ledger.Actions.Reverse()] };

		// Ledger actions record the order steps ran in, so reordering them is a real change.
		BackfillDocuments.ComputeHash(reversed).Should().NotBe(BackfillDocuments.ComputeHash(ledger));
	}

	[Fact]
	public void Compute_KnownInput_MatchesKnownSha256()
	{
		// SHA-256 of the ASCII bytes of "{}" — pins the algorithm and the output format.
		BackfillHash.Compute("{}").Should()
			.Be("sha256:44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a");
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("sha256:", false)]
	[InlineData("sha256:abc", false)]
	[InlineData("md5:44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a", false)]
	[InlineData("sha256:44136FA355B3678A1146AD16F7E8649E94FB4FC21FE77E8310C060F61CAAFF8A", false)]
	[InlineData("sha256:44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a", true)]
	public void IsWellFormed_RecognizesOnlyLowercaseSha256(string? value, bool expected) =>
		BackfillHash.IsWellFormed(value).Should().Be(expected);
}
