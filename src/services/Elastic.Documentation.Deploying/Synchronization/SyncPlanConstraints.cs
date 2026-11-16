// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Deploying.Synchronization;

/// <summary>Every rule a <see cref="SyncPlan"/> must satisfy before it is applied, in the order they are reported.</summary>
[Constraints]
public static partial class SyncPlanConstraints
{
	public static Constraint<SyncPlan> RemoteListingComplete { get; } = Constraints<SyncPlan>.Always(
		p => p.RemoteListingCompleted,
		"Remote files were not read to completion, cannot validate deployment plan"
	).Describe(
		"P29: The remote listing was read to completion; a partial listing would plan deletions or uploads against a bucket it has not seen."
	);

	public static Constraint<SyncPlan> SourceFilesPresent { get; } = Constraints<SyncPlan>.Always(
		p => p.TotalSourceFiles > 0,
		"No files to sync"
	).Describe("P29: The build output has at least one file to sync; an empty output would delete everything.");

	public static Constraint<SyncPlan> DeleteRatioWithinThreshold { get; } = Constraints<SyncPlan>.Check(
		p => p.DeleteRatio > p.DeleteThreshold
			? [Violation.Error($"Delete ratio is {p.DeleteRatio} which is greater than the threshold of {p.DeleteThreshold}")]
			: []
	).Describe(
		"P31 (P30): The share of remote files the plan deletes does not exceed the delete threshold, which is raised for small buckets and zero for an empty one."
	);

	public static Constraint<SyncPlan> TotalSyncRequestsIsAddUpdateDelete { get; } = Constraints<SyncPlan>
		.Always(
			p => p.TotalSyncRequests == p.AddRequests.Count + p.UpdateRequests.Count + p.DeleteRequests.Count,
			"total_sync_requests must be add + update + delete, but the plan counts its skipped files too."
		)
		.WithSeverity(Severity.Warning)
		.Describe("P36: total_sync_requests is the sum of the add, update and delete requests, as it is documented.");
}
