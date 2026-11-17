// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Sync;
using Nullean.Entities;

namespace Elastic.Documentation.Tests.Domain;

public class SyncPlanCountsTests
{
	private static SyncPlanCounts Counts(
		bool completed = true,
		float? threshold = null,
		int source = 10,
		int remote = 2000,
		int deletes = 0
	) => new(completed, threshold, source, remote, deletes);

	private static IReadOnlyList<string?> Errors(SyncPlanCounts counts) =>
		[.. SyncPlanCounts.Constraints.Report(counts).Violations.Where(v => v.Severity == Severity.Error).Select(v => v.ConstraintId)];

	[Test]
	public void AHealthyPlan_BreaksNothing() => Errors(Counts()).Should().BeEmpty();

	[Test]
	public void APartialListing_IsAnError() =>
		SyncPlanCounts
			.Constraints
			.Report(Counts(completed: false))
			.Violations
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Be("Remote files were not read to completion, cannot validate deployment plan");

	[Test]
	public void AnEmptyBuild_IsAnError() =>
		SyncPlanCounts
			.Constraints
			.Report(Counts(source: 0))
			.Violations
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Be("No files to sync");

	[Test]
	public void DeletingMoreThanTheThreshold_IsAnError() =>
		SyncPlanCounts
			.Constraints
			.Report(Counts(remote: 2000, deletes: 401))
			.Violations
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.StartWith("Delete ratio is ");

	[Test]
	public void DeletingExactlyTheThreshold_IsAllowed() => Errors(Counts(remote: 2000, deletes: 400)).Should().BeEmpty();

	[Test]
	[Arguments(0, 0f)]
	[Arguments(1, 0.8f)]
	[Arguments(100, 0.8f)]
	[Arguments(101, 0.5f)]
	[Arguments(1000, 0.5f)]
	[Arguments(1001, 0.2f)]
	public void TheThreshold_IsRaisedForSmallBuckets(int remote, float expected) =>
		Counts(remote: remote).DeleteThreshold.Should().Be(expected);

	[Test]
	public void AUserThresholdAboveTheMinimum_Wins() => Counts(threshold: 0.9f, remote: 50).DeleteThreshold.Should().Be(0.9f);

	[Test]
	public void AnEmptyBucket_HasNothingToDelete() => Errors(Counts(remote: 0, deletes: 1)).Should().ContainSingle();
}
