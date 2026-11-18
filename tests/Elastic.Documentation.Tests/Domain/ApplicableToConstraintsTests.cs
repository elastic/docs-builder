// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Versions;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class ApplicableToConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	/// <summary>Every rule and every operation of <see cref="ApplicableTo"/>, one generated test per kind of rule, with a block that breaks each rule.</summary>
	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest.For(ApplicableToConstraints.All, Generated).Seed(RunSeed.Value).CounterExamples(CounterExamples).AsDataSource();

	private static readonly ProductLifecycle[] Current =
	[
		ProductLifecycle.TechnicalPreview,
		ProductLifecycle.Experimental,
		ProductLifecycle.Beta,
		ProductLifecycle.GenerallyAvailable,
		ProductLifecycle.Deprecated,
		ProductLifecycle.Removed
	];

	private static Gen<SemVersion> Versions { get; } = Gen.Select(
		Gen.Int[0, 20],
		Gen.Int[0, 9],
		Gen.Int[0, 9],
		(major, minor, patch) => new SemVersion(major, minor, patch)
	);

	private static Item Of(ProductLifecycle lifecycle, string? version) =>
		new(lifecycle, version is null ? null : VersionSpec.TryParse(version, out var v) ? v : null);

	private sealed record Item(ProductLifecycle Lifecycle, VersionSpec? Version);

	private static AppliesCollection Collection(params Item[] items) =>
		AppliesCollection.Of(items.Select(i => new Applicability { Lifecycle = i.Lifecycle, Version = i.Version }));

	private static AppliesCollection Versionless => Collection(Of(ProductLifecycle.GenerallyAvailable, null));

	/// <summary>
	/// An applies_to block as the reader builds it: each key one item with a version, the keys that take no version (ess and the serverless
	/// projects) written without one, and at least one key.
	/// </summary>
	private static Gen<ApplicableTo> Generated =>
		GenAuto.Of<ApplicableTo>(AutoMode.Plausible, o => o
			.Use(
				Gen.Select(
					Gen.OneOf(Current),
					Versions,
					(lifecycle, version) => Collection(Of(lifecycle, null) with { Version = VersionSpec.GreaterThanOrEqual(version) })
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
				var cleaned = a with { Diagnostics = null };
				return ApplicableToConstraints.Collections(cleaned).Any()
					? cleaned
					: cleaned with { Stack = Collection(Of(ProductLifecycle.GenerallyAvailable, "9.0")) };
			}));

	private static IReadOnlyDictionary<string, Func<ApplicableTo, ApplicableTo>> CounterExamples =>
		new Dictionary<string, Func<ApplicableTo, ApplicableTo>>
		{
			["versionless-keys-have-no-version"] =
				a =>
					a with
					{
						Deployment = (a.Deployment ?? new DeploymentApplicability()) with
						{
							Ess = Collection(Of(ProductLifecycle.GenerallyAvailable, "9.0"))
						}
					},
			["applicability-versions-consistent"] =
				a => a with { Stack = Collection(Of(ProductLifecycle.Beta, "=9.0"), Of(ProductLifecycle.Beta, "=9.1")) }
		};

	private static IReadOnlyList<string> Messages(ApplicableTo applicableTo) =>
		[.. ApplicableToConstraints.All.Validate(applicableTo).Select(v => v.Message)];

	[Test]
	public void ValidApplicability_BreaksNoRule() =>
		Messages(ApplicableTo.ForStack(ProductLifecycle.GenerallyAvailable, VersionSpec.GreaterThanOrEqual(new SemVersion(9, 0, 0))))
			.Should()
			.BeEmpty();

	[Test]
	public void VersionlessKey_WithAVersion_ReportsTheSentenceTheReaderUses() =>
		Messages(new ApplicableTo
		{
			Deployment = new DeploymentApplicability { Ess = Collection(Of(ProductLifecycle.GenerallyAvailable, "9.0")) }
		})
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(
				"Can't specify a version for 'ess' because this product is not versioned. Remove the version, or use 'stack:' for version-specific requirements."
			);

	[Test]
	public void OneKey_RepeatingALifecycleVersion_ReportsItOnce() =>
		Messages(ApplicableTo.ForStack(Collection(Of(ProductLifecycle.Beta, "=9.0"), Of(ProductLifecycle.Beta, "=9.1"))))
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("Key 'stack': Multiple version declarations found for lifecycle(s): Beta. Only one version per lifecycle is allowed.");

	[Test]
	public void OneKey_WithAnInvertedRange_NamesTheRange() =>
		Messages(ApplicableTo.ForStack(Collection(Of(ProductLifecycle.GenerallyAvailable, "9.5-9.1"))))
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("Key 'stack': Invalid range(s) where first version is greater than last version: GenerallyAvailable (9.5-9.1).");

	[Test]
	public void OneKey_WithOverlappingRanges_ReportsThem() =>
		Messages(
			ApplicableTo.ForStack(Collection(Of(ProductLifecycle.Beta, "9.0-9.3"), Of(ProductLifecycle.GenerallyAvailable, "9.2-9.5")))
		)
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("Key 'stack': Overlapping version ranges detected. Ensure version ranges do not overlap within the same key.");

	[Test]
	public void OneKey_ARangeEndingWhereAVersionBumpStarts_Overlaps_Nothing() =>
		Messages(ApplicableTo.ForStack(Collection(Of(ProductLifecycle.Beta, "9.0-9.2"), Of(ProductLifecycle.GenerallyAvailable, "9.2+"))))
			.Should()
			.BeEmpty();

	/// <summary>
	/// Law: the lifecycle machine and the rank agree: every move the machine allows goes forward, and its diagram is drawn from the declaration.
	/// Consequence if broken: the documented lifecycle allows a move that its own order forbids.
	/// </summary>
	[Test]
	public void TheLifecycleMachineAndItsRankAgree()
	{
		var machine = ProductLifecycleMachine.Machine;

		machine.Transitions.Should().NotBeEmpty();
		machine
			.Transitions
			.Should()
			.OnlyContain(t => ProductLifecycleMachine.Rank(t.From) < ProductLifecycleMachine.Rank(t.To), "a lifecycle only moves forward");
		machine.ToMermaid().Should().StartWith("stateDiagram-v2").And.Contain("GenerallyAvailable").And.Contain("Deprecated");
		ProductLifecycleMachine.Rank(ProductLifecycle.Unavailable).Should().BeNull();
		ProductLifecycleMachine
			.Rank(ProductLifecycle.TechnicalPreview)
			.Should()
			.Be(ProductLifecycleMachine.Rank(ProductLifecycle.Experimental));
	}

	[Test]
	public void WithoutDiagnostics_DropsOnlyTheDiagnostics()
	{
		var withDiagnostics = ApplicableTo.ForStack(ProductLifecycle.Beta, null) with
		{
			Diagnostics = new ApplicabilityDiagnosticsCollection([(Elastic.Documentation.Diagnostics.Severity.Hint, "h")])
		};

		var cleaned = withDiagnostics.WithoutDiagnostics();

		cleaned.Diagnostics.Should().BeNull();
		cleaned.Stack.Should().Equal(withDiagnostics.Stack!);
	}
}
