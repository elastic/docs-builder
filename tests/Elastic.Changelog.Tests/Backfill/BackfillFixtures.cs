// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Backfill;

namespace Elastic.Changelog.Tests.Backfill;

/// <summary>
/// Representative, valid instances of all six backfill document families. Each fixture
/// fills every field at least once so round-trip tests exercise the full shape.
/// </summary>
public static class BackfillFixtures
{
	public const string SampleHash = "sha256:9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

	public static InventoryDocument Inventory() => new()
	{
		Sources =
		[
			new InventorySource
			{
				SourceRepository = new GitRepository { Owner = "elastic", Name = "elasticsearch" },
				GitRef = "main",
				Docset = "docs-content",
				Paths = ["docs/release-notes/index.md", "docs/release-notes/breaking-changes.md"],
				ProductIds = ["elasticsearch"],
				TargetScheme = TargetScheme.Semver,
				Cutoff = new BackfillCutoff { Kind = CutoffKind.Version, Value = "9.0.0", Notes = "Stack products start at 9.0" },
				Substitutions = new Dictionary<string, string> { ["es"] = "Elasticsearch", ["kib"] = "Kibana" },
				LinkMappings = new Dictionary<string, string> { ["./breaking-changes.md"] = "https://www.elastic.co/docs/release-notes/elasticsearch/breaking-changes" },
				AttributedRepositories =
				[
					new AttributedRepository
					{
						Repository = new GitRepository { Owner = "elastic", Name = "elasticsearch" },
						OnScrubberAllowlist = true
					}
				],
				DefaultRepository = new GitRepository { Owner = "elastic", Name = "elasticsearch" },
				BundleFilenameConvention = "{repo}-{target}.yaml",
				AdoptionState = AdoptionState.NotAdopted,
				Classification = SourceClassification.PublishedHistoryFound,
				AppliedOverrideIds = ["fix-9-0-1-release-date"],
				UnresolvedItems = ["The 9.0.2 section mixes known issues with fixes; needs a human decision."]
			},
			new InventorySource
			{
				SourceRepository = new GitRepository { Owner = "elastic", Name = "cloud" },
				GitRef = "master",
				ProductIds = ["cloud-hosted"],
				TargetScheme = TargetScheme.Monthly,
				AdoptionState = AdoptionState.PartiallyAdopted,
				Classification = SourceClassification.HybridPage
			}
		]
	};

	public static OverridesDocument Overrides() => new()
	{
		Overrides =
		[
			new BackfillOverride
			{
				Id = "fix-9-0-1-release-date",
				Scope = new OverrideScope
				{
					Product = "elasticsearch",
					Repository = new GitRepository { Owner = "elastic", Name = "elasticsearch" },
					Target = "9.0.1"
				},
				Path = "release_date",
				Operation = OverrideOperation.Set,
				Value = "2025-05-06",
				Reason = "The published page has no date; the GitHub release for v9.0.1 says 2025-05-06."
			},
			new BackfillOverride
			{
				Id = "drop-duplicated-entry",
				Scope = new OverrideScope { Product = "elasticsearch", Target = "9.0.0" },
				Path = "entries[12]",
				Operation = OverrideOperation.Remove,
				Reason = "Duplicate of entries[4]; same PR, same text, listed under two areas."
			}
		]
	};

	public static SemanticModelDocument SemanticModel() => new()
	{
		Releases =
		[
			new ProductRelease
			{
				Product = "elasticsearch",
				Target = "9.0.0",
				ReleaseDate = new DateOnly(2025, 4, 8),
				Description = "First release of the 9.x series.",
				Entries =
				[
					new ReleaseEntry
					{
						CategoryFamily = EntryCategoryFamily.FeaturesAndEnhancements,
						PreciseType = PreciseEntryType.Feature,
						Title = "Add better binary quantization to dense vectors",
						Description = "Dense vector fields now support the `bbq_hnsw` index type.",
						Highlight = true,
						ProductReferences =
						[
							new ReleaseProductReference { Product = "elasticsearch", Target = "9.0.0", Lifecycle = ReleaseLifecycle.Ga }
						],
						Links = ["https://www.elastic.co/docs/reference/elasticsearch/dense-vector"],
						Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
						Issues = ["https://github.com/elastic/elasticsearch/issues/12000"],
						Areas = ["Vector Search"],
						Identity = EntryIdentity.ForPullRequest("elastic", "elasticsearch", 12345),
						SourceLocation = new SourceLocation { Path = "docs/release-notes/index.md", StartLine = 42, EndLine = 45 }
					},
					new ReleaseEntry
					{
						CategoryFamily = EntryCategoryFamily.BreakingChanges,
						Subtype = BreakingChangeSubtype.Configuration,
						Title = "Remove the `node.attr` legacy setting",
						Impact = "Clusters configured with `node.attr` will not start.",
						Action = "Move node attributes to the new `node.attributes` block.",
						Identity = EntryIdentity.ForFile("backfill-elasticsearch-9.0.0-0002.yaml", "0e5751c026e543b2e8ab2eb06099daa1d1e5df47778f7787faab45cdf12fe3a8"),
						SourceLocation = new SourceLocation { Path = "docs/release-notes/breaking-changes.md", StartLine = 10, EndLine = 18 }
					}
				]
			}
		],
		Diagnostics =
		[
			new TriageDiagnostic
			{
				Severity = TriageSeverity.Warning,
				Message = "The 'Fixes' section merges bug fixes and security fixes; entries carry the family only.",
				Product = "elasticsearch",
				Target = "9.0.0",
				Location = new SourceLocation { Path = "docs/release-notes/index.md", StartLine = 60 }
			}
		]
	};

	public static PlanDocument Plan() => new()
	{
		Scope = new PlanScope
		{
			Product = "elasticsearch",
			Repository = new GitRepository { Owner = "elastic", Name = "elasticsearch" },
			TargetRange = "9.0.0..9.0.2"
		},
		SourceRefs =
		[
			new PinnedSource
			{
				Repository = new GitRepository { Owner = "elastic", Name = "elasticsearch" },
				GitRef = "main",
				Commit = "0123456789abcdef0123456789abcdef01234567"
			}
		],
		InventoryHash = SampleHash,
		SemanticModelHash = SampleHash,
		OverridesHash = SampleHash,
		EnrichmentSnapshotHash = SampleHash,
		ScrubberAllowlist = new ScrubberAllowlist
		{
			Sha256 = SampleHash,
			DeploymentCommit = "89abcdef0123456789abcdef0123456789abcdef"
		},
		CurrentState =
		[
			new RemoteObject
			{
				Key = "bundle/elasticsearch/elasticsearch-9.0.2.yaml",
				ETag = "\"d41d8cd98f00b204e9800998ecf8427e\"",
				Sha256 = SampleHash
			}
		],
		Actions =
		[
			new PlanAction
			{
				Kind = PlanActionKind.CreateBundle,
				Product = "elasticsearch",
				Target = "9.0.0",
				Key = "bundle/elasticsearch/elasticsearch-9.0.0.yaml",
				ContentSha256 = SampleHash
			},
			new PlanAction
			{
				Kind = PlanActionKind.CreateAmend,
				Product = "elasticsearch",
				Target = "9.0.2",
				Key = "bundle/elasticsearch/elasticsearch-9.0.2.amend-1.yaml",
				ContentSha256 = SampleHash,
				ParentKey = "bundle/elasticsearch/elasticsearch-9.0.2.yaml"
			},
			new PlanAction
			{
				Kind = PlanActionKind.SkipExisting,
				Product = "elasticsearch",
				Target = "9.0.1",
				Key = "bundle/elasticsearch/elasticsearch-9.0.1.yaml",
				Reason = "The key already exists with exactly the bytes this plan would write."
			},
			new PlanAction
			{
				Kind = PlanActionKind.ManualReview,
				Product = "elasticsearch",
				Target = "9.0.3",
				Reason = "Two bundles claim this target and neither is an unambiguous amend parent."
			}
		]
	};

	public static ProvenanceDocument Provenance() => new()
	{
		Records =
		[
			new ProvenanceRecord
			{
				Product = "elasticsearch",
				Target = "9.0.0",
				Entry = EntryIdentity.ForPullRequest("elastic", "elasticsearch", 12345),
				Field = "precise_type",
				Value = "feature",
				Source = EvidenceSource.GithubMetadata,
				Confidence = EvidenceConfidence.High,
				Evidence = "PR label '>feature' via changelog.yml label mapping"
			},
			new ProvenanceRecord
			{
				Product = "elasticsearch",
				Target = "9.0.0",
				Field = "release_date",
				Value = "2025-04-08",
				Source = EvidenceSource.ReleaseNoteSource,
				Confidence = EvidenceConfidence.Medium,
				Evidence = "Date printed under the release heading",
				Location = new SourceLocation { Path = "docs/release-notes/index.md", StartLine = 40 }
			}
		]
	};

	public static LedgerDocument Ledger() => new()
	{
		PlanHash = SampleHash,
		InputRefs =
		[
			new PinnedSource
			{
				Repository = new GitRepository { Owner = "elastic", Name = "elasticsearch" },
				GitRef = "main",
				Commit = "0123456789abcdef0123456789abcdef01234567"
			}
		],
		CreatedObjectHashes = new Dictionary<string, string>
		{
			["bundle/elasticsearch/elasticsearch-9.0.0.yaml"] = SampleHash
		},
		Actions =
		[
			new LedgerAction
			{
				PlannedKind = PlanActionKind.CreateBundle,
				Key = "bundle/elasticsearch/elasticsearch-9.0.0.yaml",
				Outcome = LedgerActionOutcome.Created
			},
			new LedgerAction
			{
				PlannedKind = PlanActionKind.CreateAmend,
				Key = "bundle/elasticsearch/elasticsearch-9.0.2.amend-1.yaml",
				Outcome = LedgerActionOutcome.Conflict,
				Detail = "The key appeared between planning and apply, with different bytes."
			}
		],
		RegistryState =
		[
			new RegistryRefresh
			{
				Key = "bundle/elasticsearch/registry.json",
				Outcome = RegistryRefreshOutcome.Updated
			}
		],
		Verification = new VerificationResult
		{
			Outcome = VerificationOutcome.Failed,
			Details = ["9.0.2: one planned entry is missing from the public bundle (conflict above was never applied)."]
		},
		StartedAt = new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero),
		FinishedAt = new DateTimeOffset(2026, 7, 20, 12, 5, 30, TimeSpan.Zero)
	};
}
