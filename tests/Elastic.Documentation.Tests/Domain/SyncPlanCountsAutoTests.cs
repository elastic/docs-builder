// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Sync;
using Nullean.Entities;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// Generated sync plan counts against every rule of <see cref="SyncPlanCounts"/>. The record has no operation that returns a new one (the planner builds
/// it from a listing), so the operation axes have nothing to run: the invariant, absence and vacancy axes cover the rules, over two generators.
/// <see cref="Legal"/> is every plan a deploy may apply; <see cref="Hostile"/> adds partial listings, empty builds, negative, zero, huge and boundary
/// counts, and thresholds that are negative, infinite or NaN, so the ratio is NaN or infinite.
/// </summary>
public class SyncPlanCountsAutoTests
{
	private static readonly int[] EdgeCounts =
	[
		int.MinValue,
		-1000,
		-1,
		0,
		1,
		2,
		99,
		100,
		101,
		999,
		1000,
		1001,
		1_000_000,
		int.MaxValue - 1,
		int.MaxValue
	];

	private static readonly float?[] EdgeThresholds =
	[
		null,
		null,
		0f,
		-0.5f,
		0.2f,
		0.5f,
		0.8f,
		1f,
		2f,
		float.Epsilon,
		float.MaxValue,
		float.MinValue,
		float.NaN,
		float.PositiveInfinity,
		float.NegativeInfinity
	];

	private static readonly Gen<int> Counts = Gen.Frequency((2, Gen.OneOf(EdgeCounts)), (2, Gen.Int[0, 5000]), (1, Gen.Int));

	private static readonly Gen<SyncPlanCounts> Hostile = Gen.Select(
		Gen.Bool,
		Gen.OneOf(EdgeThresholds),
		Counts,
		Counts,
		Counts,
		(completed, threshold, source, remote, deletes) => new SyncPlanCounts(completed, threshold, source, remote, deletes)
	);

	// a plan a deploy may apply: complete listing, files to sync, and deletions within the threshold the plan itself computes
	private static readonly Gen<SyncPlanCounts> Legal = Gen.Select(
		Gen.OneOf<float?>(null, 0.2f, 0.5f, 0.9f, 1f),
		Gen.Int[1, 100_000],
		Gen.Frequency((3, Gen.Int[0, 3000]), (1, Gen.OneOf(0, 1, 100, 101, 1000, 1001))),
		Gen.Float[0f, 0.2f],
		(threshold, source, remote, share) => new SyncPlanCounts(true, threshold, source, remote, (int)(remote * share))
	).Where(c => SyncPlanCounts.Constraints.Report(c).Violations.Count == 0);

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest.For(SyncPlanCountsConstraints.All, Legal).Seed(RunSeed.Value).CounterExamples(CounterExamples).AsDataSource();

	private static IReadOnlyDictionary<string, Func<SyncPlanCounts, SyncPlanCounts>> CounterExamples
	{
		get;
	} = new Dictionary<string, Func<SyncPlanCounts, SyncPlanCounts>>
	{
		["remote-listing-complete"] = c => c with { RemoteListingCompleted = false },
		["source-files-present"] = c => c with { TotalSourceFiles = 0 },
		["delete-ratio-within-threshold"] = c => c with { DeleteThresholdDefault = null, TotalRemoteFiles = 2000, DeleteCount = 2000 }
	};

	/// <summary>
	/// Law: every rule of the sync plan holds for every legal plan the generator builds, and every rule can be broken by some mutation of one.
	/// Consequence if broken: a rule that cannot fire reports green while checking nothing, and a deploy applies a plan nothing checked.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	/// <summary>
	/// Law: no rule, ratio or threshold throws for any counts, however hostile (zero remote files, negative counts, a NaN threshold).
	/// Consequence if broken: a bad listing crashes the deploy step instead of being reported.
	/// </summary>
	[Test]
	public void HostileCounts_NeverThrow() =>
		Hostile.Sample(
			c =>
			{
				_ = SyncPlanCounts.Constraints.Report(c);
				_ = c.DeleteRatio;
				_ = c.DeleteThreshold;
				_ = c.DeleteThresholdReason;
				return true;
			},
			seed: RunSeed.Value,
			iter: 20_000
		);

	[Test]
	public void HostileCounts_BreakEveryRuleAndSomeBreakNone()
	{
		var broken = new HashSet<string?>();
		var legal = 0;
		Hostile.Sample(
			c =>
			{
				var violations = SyncPlanCounts.Constraints.Report(c).Violations;
				foreach (var v in violations)
					_ = broken.Add(v.ConstraintId);
				if (violations.Count == 0)
					legal++;
				return true;
			},
			seed: RunSeed.Value,
			iter: 20_000
		);

		broken.Should().BeEquivalentTo(["remote-listing-complete", "source-files-present", "delete-ratio-within-threshold"]);
		legal.Should().BeGreaterThan(0);
	}

	[Test]
	public void ANanOrInfiniteRatio_IsReportedAsFoundWhenItExceedsTheThreshold()
	{
		var deletesFromNothing = new SyncPlanCounts(true, null, 1, 0, 3);
		var nothingToDelete = new SyncPlanCounts(true, null, 1, 0, 0);

		deletesFromNothing.DeleteRatio.Should().Be(float.PositiveInfinity);
		SyncPlanCounts.Constraints.Report(deletesFromNothing).Violations.Should().ContainSingle();
		float.IsNaN(nothingToDelete.DeleteRatio).Should().BeTrue();
		SyncPlanCounts.Constraints.Report(nothingToDelete).Violations.Should().BeEmpty();
	}

	// main compares the same way (ratio > threshold), so a NaN threshold never rejects a plan; pinned so a change to that is a decision
	[Test]
	public void ANanThreshold_NeverRejectsADeletion() =>
		SyncPlanCounts.Constraints.Report(new SyncPlanCounts(true, float.NaN, 1, 2000, 2000)).Violations.Should().BeEmpty();
}
