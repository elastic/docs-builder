// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Sync;

/// <summary>
/// The numbers of an S3 sync plan that decide whether a deploy may run: whether the remote listing was complete, how many files the build and the bucket
/// hold, and how many of the bucket's files the plan deletes.
/// </summary>
public sealed record SyncPlanCounts(
	bool RemoteListingCompleted,
	float? DeleteThresholdDefault,
	int TotalSourceFiles,
	int TotalRemoteFiles,
	int DeleteCount
) : IHaveConstraints<SyncPlanCounts>
{
	/// <summary>The delete threshold when none was given: no more than a fifth of the remote files may be deleted.</summary>
	public const float DefaultDeleteThreshold = 0.2f;

	public static ConstraintSet<SyncPlanCounts> Constraints => SyncPlanCountsConstraints.All;

	/// <summary>The fraction of the remote files this plan deletes.</summary>
	public float DeleteRatio => (float)DeleteCount / TotalRemoteFiles;

	/// <summary>
	/// The fraction of the remote files this plan may delete: <see cref="DeleteThresholdDefault"/> (or 0.2), raised to 0.8 for at most 100 remote files
	/// and to 0.5 for at most 1,000 so a young bucket can change quickly, and 0 for an empty bucket, which has nothing to delete.
	/// </summary>
	public float DeleteThreshold => Threshold().Value;

	/// <summary>Why <see cref="DeleteThreshold"/> differs from the default, or null when it does not.</summary>
	public string? DeleteThresholdReason => Threshold().Reason;

	private (float Value, string? Reason) Threshold()
	{
		var threshold = DeleteThresholdDefault ?? DefaultDeleteThreshold;
		if (TotalRemoteFiles == 0)
		{
			return (0.0f, "No files discovered in S3, assuming a clean bucket resetting delete threshold to `0.0' as our plan should not have ANY deletions");
		}
		// a plan against 100 remote files or fewer is in flux, so it may delete up to 0.8
		if (TotalRemoteFiles <= 100)
			return (Math.Max(threshold, 0.8f), "Plan has less than 100 total remote files ensuring delete threshold is at minimum 0.8");
		if (TotalRemoteFiles <= 1000)
			return (Math.Max(
				threshold,
				0.5f
			), "Plan has less than 1000 but more than a 100 total remote files ensuring delete threshold is at minimum 0.5");
		return (threshold, null);
	}
}

/// <summary>Every rule a <see cref="SyncPlanCounts"/> must satisfy before a plan is applied, in the order they are reported.</summary>
[Constraints<SyncPlanCounts>]
public static partial class SyncPlanCountsConstraints
{
	public static readonly Constraint<SyncPlanCounts> RemoteListingComplete = Constraints<SyncPlanCounts>.Always(
		p => p.RemoteListingCompleted,
		"Remote files were not read to completion, cannot validate deployment plan"
	).Describe(
		"The remote listing was read to completion; a partial listing would plan deletions or uploads against a bucket it has not seen."
	);

	public static readonly Constraint<SyncPlanCounts> SourceFilesPresent = Constraints<SyncPlanCounts>.Always(
		p => p.TotalSourceFiles != 0,
		"No files to sync"
	).Describe(
		"The build output has files to sync; an empty output would delete everything. Only zero is rejected, as the validator always did: a persisted negative count passes."
	);

	public static readonly Constraint<SyncPlanCounts> DeleteRatioWithinThreshold = Constraints<SyncPlanCounts>.Check(
		p => p.DeleteRatio > p.DeleteThreshold
			? [Violation.Error($"Delete ratio is {p.DeleteRatio} which is greater than the threshold of {p.DeleteThreshold}")]
			: []
	).Describe("The share of remote files the plan deletes does not exceed the delete threshold.");
}
