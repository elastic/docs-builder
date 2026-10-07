// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Versions;
using Nullean.Entities.CsCheck;
using DiagnosticSeverity = Elastic.Documentation.Diagnostics.Severity;

namespace Elastic.Documentation.Tests.Domain;

public class ApplicableToConstraintsTests
{
	[Test]
	public void Laws() => ApplicableToConstraints.All.Laws(Generated, CounterExamples);

	private static readonly ProductLifecycle[] Current =
	[
		ProductLifecycle.TechnicalPreview,
		ProductLifecycle.Experimental,
		ProductLifecycle.Beta,
		ProductLifecycle.GenerallyAvailable,
		ProductLifecycle.Deprecated,
		ProductLifecycle.Removed
	];

	private static AppliesCollection Versionless => Collection(Item(ProductLifecycle.GenerallyAvailable, null));

	/// <summary>
	/// An applies_to block as the reader builds it: each key one item with a current lifecycle (not a deprecated spelling) and a version, the keys that take no version (ess and the
	/// serverless projects) written without one, nothing unread or ignored, and at least one key.
	/// </summary>
	private static Gen<ApplicableTo> Generated =>
		DomainGen.Of<ApplicableTo>(o => o
			.Use(
				Gen.Select(
					Gen.OneOfConst(Current),
					DomainGen.SemVersions,
					(lifecycle, version) => Collection(new Applicability
					{
						Lifecycle = lifecycle,
						Version = VersionSpec.GreaterThanOrEqual(version)
					})
				)
			)
			.Normalize<DeploymentApplicability>(d => d with { Ess = d.Ess is null ? null : Versionless })
			.Normalize<ServerlessProjectApplicability>(
				s =>
					s with
					{
						Elasticsearch = s.Elasticsearch is null ? null : Versionless,
						Observability = s.Observability is null ? null : Versionless,
						Security = s.Security is null ? null : Versionless,
						VectorDatabase = s.VectorDatabase is null ? null : Versionless
					}
			)
			.Normalize<ApplicableTo>(a =>
			{
				var cleaned = a with { UnknownKeys = [], IgnoredValues = [], Diagnostics = new ApplicabilityDiagnosticsCollection([]) };
				return ApplicableToConstraints.Collections(cleaned).Any()
					? cleaned
					: cleaned with { Stack = Collection(Item(ProductLifecycle.GenerallyAvailable, "9.0")) };
			}));

	[Test]
	public void ValidApplicability_BreaksNoRule() => ApplicableToConstraints.All.Validate(Valid()).Should().BeEmpty();

	[Test]
	public void DiagnosticsAreReportedAtTheirOwnSeverity()
	{
		var applicable = Valid() with
		{
			Diagnostics = new ApplicabilityDiagnosticsCollection([
				(DiagnosticSeverity.Error, "e"),
				(DiagnosticSeverity.Warning, "w"),
				(DiagnosticSeverity.Hint, "h")
			])
		};
		ApplicableToConstraints
			.All
			.Validate(applicable)
			.Select(v => v.Severity)
			.Should()
			.Equal(Nullean.Entities.Severity.Error, Nullean.Entities.Severity.Warning, Nullean.Entities.Severity.Hint);
	}

	[Test]
	public void EachVersionProblem_IsReportedOnItsOwn()
	{
		var gte = Collection(Item(ProductLifecycle.Beta, "9.1"), Item(ProductLifecycle.GenerallyAvailable, "9.3"));
		ApplicableToConstraints
			.All
			.Validate(new ApplicableTo { Stack = gte })
			.Select(v => v.Message)
			.Should()
			.Contain(m => m.Contains("greater-than-or-equal", StringComparison.Ordinal));

		var inverted = Collection(Item(ProductLifecycle.GenerallyAvailable, "9.5-9.1"));
		ApplicableToConstraints
			.All
			.Validate(new ApplicableTo { Stack = inverted })
			.Select(v => v.Message)
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Contain("Invalid range");

		var overlapping = Collection(Item(ProductLifecycle.GenerallyAvailable, "9.0-9.3"), Item(ProductLifecycle.Beta, "=9.2"));
		ApplicableToConstraints
			.All
			.Validate(new ApplicableTo { Stack = overlapping })
			.Select(v => v.Message)
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Contain("Overlapping");

		var bump = Collection(Item(ProductLifecycle.Beta, "9.0-9.2"), Item(ProductLifecycle.GenerallyAvailable, "9.2"));
		ApplicableToConstraints.All.Validate(new ApplicableTo { Stack = bump }).Should().BeEmpty();
	}

	[Test]
	public void ExtraTokensAfterTheVersion_AreReported()
	{
		var item = new Applicability
		{
			Lifecycle = ProductLifecycle.Beta,
			Version = VersionSpec.TryParse("9.1", out var v) ? v : null,
			Raw = "beta 9.1 extra"
		};
		ApplicableToConstraints
			.All
			.Validate(new ApplicableTo { Stack = Collection(item) })
			.Select(x => x.Message)
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Contain("'extra'");
	}

	[Test]
	public void TheReadStage_LeavesTheReadersOwnWording_OfM94AndM96_ToTheReader()
	{
		var asked = ApplicableToReadConstraints.All.AsConstraints().Select(r => r.Name).ToList();

		asked.Should().NotContain(ApplicableToConstraints.VersionlessKeysHaveNoVersion.Name);
		asked.Should().NotContain(ApplicableToConstraints.ApplicabilityVersionsConsistent.Name);
		asked.Should().Contain(ApplicableToConstraints.ApplicabilityLifecycleNotDeprecated.Name);
		asked.Should().Contain(ApplicableToConstraints.ApplicabilityDiagnosticsReported.Name);
	}

	[Test]
	public void AFindingTheReaderMadeForAnotherRulesProblem_IsReportedUnderThatRulesId_Once()
	{
		var applicable = Valid() with
		{
			Diagnostics = new ApplicabilityDiagnosticsCollection([
				new ApplicabilityDiagnostic(
					DiagnosticSeverity.Error,
					"Can't specify a version for 'ech'",
					ApplicableToConstraints.VersionlessKeysHaveNoVersion.Name
				),
				new ApplicabilityDiagnostic(DiagnosticSeverity.Warning, "Applies block does not support the following keys: hosted")
			])
		};

		var found = ApplicableToReadConstraints.Check(applicable);

		found.Should().HaveCount(2);
		found[0].RuleId.Should().Be(ApplicableToConstraints.VersionlessKeysHaveNoVersion.Name);
		found[0].Message.Should().Be("Can't specify a version for 'ech'");
		found[1].RuleId.Should().Be(ApplicableToConstraints.ApplicabilityDiagnosticsReported.Name);
	}

	[Test]
	public void ADeprecatedLifecycle_IsNamedAsWritten_AndEveryDeprecatedOneIsReported()
	{
		foreach (var written in new[] { "dev", "development", "coming", "planned", "discontinued", "unavailable" })
		{
			var item = new Applicability { Lifecycle = ProductLifecycle.Planned, Version = AllVersionsSpec.Instance, Raw = written };
			var lifecycle = written switch
			{
				"dev" or "development" => ProductLifecycle.Development,
				"discontinued" => ProductLifecycle.Discontinued,
				"unavailable" => ProductLifecycle.Unavailable,
				_ => ProductLifecycle.Planned
			};
			var found = ApplicableToReadConstraints.Check(new ApplicableTo { Stack = Collection(item with { Lifecycle = lifecycle }) });

			found
				.Should()
				.ContainSingle()
				.Which
				.Message
				.Should()
				.StartWith($"The '{written}' lifecycle is deprecated and will be removed in a future release");
		}
	}

	[Test]
	public void ABlockTheReaderAlreadyReportedOn_IsNotReportedAsEmpty()
	{
		var onlyUnknownKeys = new ApplicableTo
		{
			Diagnostics = new ApplicabilityDiagnosticsCollection([
				new ApplicabilityDiagnostic(DiagnosticSeverity.Warning, "Applies block does not support the following keys: hosted")
			])
		};

		ApplicableToReadConstraints
			.Check(onlyUnknownKeys)
			.Should()
			.ContainSingle()
			.Which
			.RuleId
			.Should()
			.Be(ApplicableToConstraints.ApplicabilityDiagnosticsReported.Name);
		ApplicableToConstraints
			.All
			.Validate(new ApplicableTo())
			.Should()
			.Contain(v => v.ConstraintId == ApplicableToConstraints.AppliesToNotEmpty.Name);
	}

	private static Applicability Item(ProductLifecycle lifecycle, string? version) =>
		new()
		{
			Lifecycle = lifecycle,
			Version = version is null ? AllVersionsSpec.Instance : VersionSpec.TryParse(version, out var v) ? v : null
		};

	private static AppliesCollection Collection(params Applicability[] items) => new(items);

	private static ApplicableTo Valid() =>
		new()
		{
			Stack = Collection(Item(ProductLifecycle.GenerallyAvailable, "9.0")),
			Deployment = new DeploymentApplicability { Ess = Collection(Item(ProductLifecycle.GenerallyAvailable, null)) },
			Serverless = ServerlessProjectApplicability.All
		};

	private static IReadOnlyDictionary<string, Func<ApplicableTo, ApplicableTo>> CounterExamples
	{
		get;
	} = new Dictionary<string, Func<ApplicableTo, ApplicableTo>>
	{
		["applies-to-not-empty"] =
			a => a with { Stack = null, Deployment = null, Serverless = null, Product = null, ProductApplicability = null },
		["applicability-lifecycle-not-deprecated"] = a => a with { Stack = Collection(Item(ProductLifecycle.Planned, "9.0")) },
		["versionless-keys-have-no-version"] =
			a => a with { Deployment = new DeploymentApplicability { Ess = Collection(Item(ProductLifecycle.GenerallyAvailable, "9.1")) } },
		["applicability-versions-consistent"] =
			a =>
				a with
				{
					Stack = Collection(Item(ProductLifecycle.GenerallyAvailable, "=9.0"), Item(ProductLifecycle.GenerallyAvailable, "=9.1"))
				},
		["applicability-version-readable"] =
			a => a with { Stack = Collection(new Applicability { Lifecycle = ProductLifecycle.Beta, Version = null, Raw = "beta 9.x" }) },
		["applies-to-keys-read"] = a => a with { UnknownKeys = ["product.apm-agent-java"] },
		["applies-to-values-are-text"] = a => a with { IgnoredValues = ["stack"] },
		["applicability-diagnostics-reported"] =
			a => a with { Diagnostics = new ApplicabilityDiagnosticsCollection([(DiagnosticSeverity.Warning, "Unknown key")]) }
	};
}
