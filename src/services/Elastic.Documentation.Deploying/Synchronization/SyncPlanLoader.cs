// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Deploying.Synchronization;

/// <summary>
/// The one way a sync plan becomes a record: a plan that was written to a file (or computed by the planner) is checked once against
/// <see cref="SyncPlanConstraints"/> and handed on inside a <see cref="Result{T}"/>, with every violation. The raw reader (<c>SyncPlan.Deserialize</c>)
/// is internal. <see cref="DocsSyncPlanValidator"/> turns the findings into the verdict the deploy acts on; it does not check the plan a second time.
/// </summary>
public static class SyncPlanLoader
{
	/// <summary>The name diagnostics are reported against when the plan has no file.</summary>
	public const string DefaultFile = "sync-plan";

	/// <summary>
	/// Parses and checks the plan's JSON. A text that does not parse (or is empty) is an error diagnostic and no plan; a plan that parses is always
	/// returned with every finding, even an error, because the caller decides what a failing plan stops.
	/// </summary>
	/// <param name="json">The plan file's text.</param>
	/// <param name="file">What diagnostics are reported against; the plan file's path.</param>
	public static Result<SyncPlan> Load(string json, string file = DefaultFile)
	{
		SyncPlan plan;
		try
		{
			plan = SyncPlan.Deserialize(json);
		}
		catch (Exception e) when (e is JsonException or NotSupportedException)
		{
			return Result.Failure<SyncPlan>([new Diagnostic { Severity = Severity.Error, File = file, Message = e.Message }]);
		}
		return Check(plan, file);
	}

	/// <summary>
	/// The plan a planner derived from what it found locally and remotely, checked by <see cref="SyncPlanConstraints"/> with every finding. The total of
	/// sync requests is the sum of the four lists (the skips included), so a planner cannot state one that disagrees with them. The plan is returned even
	/// when a rule of error severity is broken: <see cref="DocsSyncPlanValidator"/> turns the findings into the verdict, so a bad plan is reported once.
	/// </summary>
	public static Result<SyncPlan> Planned(
		bool remoteListingCompleted,
		float? deleteThreshold,
		string[] excludePatterns,
		int totalRemoteFiles,
		int totalSourceFiles,
		IReadOnlyList<DeleteRequest> deletes,
		IReadOnlyList<AddRequest> adds,
		IReadOnlyList<UpdateRequest> updates,
		IReadOnlyList<SkipRequest> skips,
		string file = DefaultFile
	) =>
		Check(
			new SyncPlan
			{
				RemoteListingCompleted = remoteListingCompleted,
				DeleteThresholdDefault = deleteThreshold,
				ExcludePatterns = excludePatterns,
				TotalRemoteFiles = totalRemoteFiles,
				TotalSourceFiles = totalSourceFiles,
				DeleteRequests = deletes,
				AddRequests = adds,
				UpdateRequests = updates,
				SkipRequests = skips,
				TotalSyncRequests = deletes.Count + adds.Count + updates.Count + skips.Count
			},
			file
		);

	/// <summary>Checks a plan built elsewhere (the planner's) once, reporting every violation.</summary>
	public static Result<SyncPlan> Check(SyncPlan plan, string file = DefaultFile) =>
		Result.From(SyncPlanConstraints.All.Report(plan), file);
}
