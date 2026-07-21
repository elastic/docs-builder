// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Backfill;

namespace Elastic.Changelog.Tests.Backfill;

/// <summary>
/// Documents with missing or invalid fields must be rejected with messages that say
/// where the problem is and what a valid value looks like.
/// </summary>
public class BackfillValidationTests
{
	[Fact]
	public void Deserialize_MissingRequiredJsonField_FailsWithParseError()
	{
		// A release without its required 'target' field.
		var json = /*lang=json,strict*/ """
			{
			  "artifact": "semantic-model",
			  "schema_version": 1,
			  "payload": {
			    "releases": [ { "product": "elasticsearch", "entries": [] } ],
			    "diagnostics": []
			  }
			}
			""";

		var act = () => BackfillDocuments.Deserialize<SemanticModelDocument>(json);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*'semantic-model' document could not be parsed*");
	}

	[Fact]
	public void Serialize_EmptyTitle_FailsWithLocationAndProblem()
	{
		var model = BackfillFixtures.SemanticModel();
		var release = model.Releases[0];
		var invalid = model with
		{
			Releases = [release with { Entries = [release.Entries[0] with { Title = " " }] }]
		};

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*releases[0]*entries[0]*non-empty title*");
	}

	[Fact]
	public void Serialize_BarePrNumber_IsRejectedWithExampleOfCanonicalUrl()
	{
		var model = BackfillFixtures.SemanticModel();
		var release = model.Releases[0];
		var invalid = model with
		{
			Releases = [release with { Entries = [release.Entries[0] with { Prs = ["12345"] }] }]
		};

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*full canonical URLs*https://github.com/*pull*'12345'*");
	}

	[Fact]
	public void Serialize_SetOverrideWithoutValue_IsRejected()
	{
		var invalid = new OverridesDocument
		{
			Overrides =
			[
				new BackfillOverride
				{
					Id = "broken",
					Scope = new OverrideScope { Product = "elasticsearch" },
					Path = "release_date",
					Operation = OverrideOperation.Set,
					Reason = "Testing"
				}
			]
		};

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*overrides[0]*sets a value needs the value*");
	}

	[Fact]
	public void Serialize_DuplicateOverrideIds_AreRejected()
	{
		var overrides = BackfillFixtures.Overrides();
		var invalid = overrides with
		{
			Overrides = [overrides.Overrides[0], overrides.Overrides[1] with { Id = overrides.Overrides[0].Id }]
		};

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*used by more than one override*");
	}

	[Fact]
	public void Serialize_CreateActionWithoutContentHash_IsRejected()
	{
		var plan = BackfillFixtures.Plan();
		var invalid = plan with
		{
			Actions = [plan.Actions[0] with { ContentSha256 = null }]
		};

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*actions[0]*create-bundle*hash of the content*");
	}

	[Fact]
	public void Serialize_AmendWithoutParentKey_IsRejected()
	{
		var plan = BackfillFixtures.Plan();
		var amend = plan.Actions.First(a => a.Kind == PlanActionKind.CreateAmend);
		var invalid = plan with { Actions = [amend with { ParentKey = null }] };

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*needs the key of the parent bundle*");
	}

	[Fact]
	public void Serialize_MalformedPlanInputHash_IsRejected()
	{
		var invalid = BackfillFixtures.Plan() with { InventoryHash = "sha256:nope" };

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*inventory hash*sha256:*64 lower-case hex*");
	}

	[Fact]
	public void Serialize_AllowlistWithNeitherHashNorCommit_IsRejected()
	{
		var invalid = BackfillFixtures.Plan() with { ScrubberAllowlist = new ScrubberAllowlist() };

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*scrubber_allowlist*content hash*deployment commit*");
	}

	[Fact]
	public void Serialize_TruncatedCommitSha_IsRejected()
	{
		var plan = BackfillFixtures.Plan();
		var invalid = plan with
		{
			SourceRefs = [plan.SourceRefs[0] with { Commit = "0123456" }]
		};

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*source_refs[0]*full 40-character commit SHA*");
	}

	[Fact]
	public void Serialize_NonUtcLedgerTimestamp_IsRejected()
	{
		var invalid = BackfillFixtures.Ledger() with
		{
			StartedAt = new DateTimeOffset(2026, 7, 20, 9, 0, 0, TimeSpan.FromHours(-3))
		};

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*must be in UTC*");
	}

	[Fact]
	public void Serialize_LedgerFinishingBeforeItStarted_IsRejected()
	{
		var ledger = BackfillFixtures.Ledger();
		var invalid = ledger with { FinishedAt = ledger.StartedAt.AddMinutes(-1) };

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*cannot finish before it started*");
	}

	[Fact]
	public void Serialize_FailedLedgerActionWithoutDetail_IsRejected()
	{
		var ledger = BackfillFixtures.Ledger();
		var invalid = ledger with
		{
			Actions =
			[
				new LedgerAction
				{
					PlannedKind = PlanActionKind.CreateBundle,
					Key = "bundle/elasticsearch/elasticsearch-9.0.0.yaml",
					Outcome = LedgerActionOutcome.Failed
				}
			]
		};

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*failed ledger action needs a detail*");
	}

	[Fact]
	public void Serialize_InventorySourceWithoutProducts_IsRejected()
	{
		var inventory = BackfillFixtures.Inventory();
		var invalid = inventory with
		{
			Sources = [inventory.Sources[0] with { ProductIds = [] }]
		};

		var act = () => BackfillDocuments.Serialize(invalid);

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*sources[0]*at least one product ID*");
	}

	[Fact]
	public void Serialize_ReportsAllProblemsAtOnce()
	{
		var plan = BackfillFixtures.Plan();
		var invalid = plan with
		{
			InventoryHash = "bad",
			SemanticModelHash = "also-bad"
		};

		var act = () => BackfillDocuments.Serialize(invalid);

		// Both problems in one error, so a human fixes everything in one pass.
		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*inventory hash*")
			.WithMessage("*semantic-model hash*");
	}

	[Fact]
	public void Validate_PullRequestIdentityWithFileBlock_IsRejected()
	{
		var identity = EntryIdentity.ForPullRequest("elastic", "elasticsearch", 1) with
		{
			File = new SyntheticFileIdentity { Name = "x.yaml", Checksum = "abc" }
		};

		var problems = new List<string>();
		identity.Validate(problems);

		problems.Should().ContainSingle().Which.Should().Contain("must not carry a file block");
	}

	[Fact]
	public void Validate_IdentityFactories_ProduceValidIdentities()
	{
		var identities = new[]
		{
			EntryIdentity.ForPullRequest("elastic", "elasticsearch", 12345),
			EntryIdentity.ForIssue("elastic", "apm-agent-dotnet", 42),
			EntryIdentity.ForFile("backfill-elasticsearch-9.0.0-0001.yaml", "deadbeef")
		};

		foreach (var identity in identities)
		{
			var problems = new List<string>();
			identity.Validate(problems);
			problems.Should().BeEmpty();
		}

		identities[0].Url.Should().Be("https://github.com/elastic/elasticsearch/pull/12345");
		identities[1].Url.Should().Be("https://github.com/elastic/apm-agent-dotnet/issues/42");
	}

	[Fact]
	public void Validate_SourceLocationWithBackwardsRange_IsRejected()
	{
		var location = new SourceLocation { Path = "docs/index.md", StartLine = 10, EndLine = 5 };

		var problems = new List<string>();
		location.Validate(problems);

		problems.Should().ContainSingle().Which.Should().Contain("cannot come before its start line");
	}

	[Fact]
	public void Fixtures_AreAllValid()
	{
		var problems = new List<string>();
		BackfillFixtures.Inventory().Validate(problems);
		BackfillFixtures.Overrides().Validate(problems);
		BackfillFixtures.SemanticModel().Validate(problems);
		BackfillFixtures.Plan().Validate(problems);
		BackfillFixtures.Provenance().Validate(problems);
		BackfillFixtures.Ledger().Validate(problems);

		problems.Should().BeEmpty();
	}
}
