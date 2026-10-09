// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Deploying.Synchronization;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Deploying.Tests;

public class SyncPlanConstraintsTests
{
	/// <summary>
	/// A plan as the planner writes it: the listing read to completion, files to sync, a total that is the add, update and delete requests (skips are not counted),
	/// and no more deletions than the threshold allows of the remote files.
	/// </summary>
	private static SyncPlan Planned(SyncPlan p)
	{
		var remote = Math.Max(p.TotalRemoteFiles, (p.DeleteRequests.Count * 5) + 1);
		return p with
		{
			DeleteThresholdDefault = null,
			RemoteListingCompleted = true,
			TotalSourceFiles = Math.Max(p.TotalSourceFiles, 1),
			TotalRemoteFiles = remote,
			TotalSyncRequests = p.AddRequests.Count + p.UpdateRequests.Count + p.DeleteRequests.Count
		};
	}

	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(SyncPlanConstraints.All, GenAuto.Of<SyncPlan>(AutoMode.Plausible, o => o.Normalize<SyncPlan>(Planned)))
			.CounterExamples(CounterExamples)
			.Seed(Elastic.Documentation.Tests.Domain.Props.Seed)
			.Cases
			.Select(c => (Func<AxisCase>)(() => c));

	private static IReadOnlyList<DeleteRequest> Deletes(int count) =>
		[.. Enumerable.Range(0, count).Select(i => new DeleteRequest { DestinationPath = $"old/{i}.html" })];

	private static IReadOnlyDictionary<string, Func<SyncPlan, SyncPlan>> CounterExamples =>
		new Dictionary<string, Func<SyncPlan, SyncPlan>>
		{
			["add-requests-required"] = p => p with { AddRequests = null! },
			["update-requests-required"] = p => p with { UpdateRequests = null! },
			["delete-requests-required"] = p => p with { DeleteRequests = null! },
			["delete-requests-destination-path-required"] =
				p => p with { DeleteRequests = [new DeleteRequest { DestinationPath = null! }] },
			["skip-requests-local-path-required"] =
				p => p with { SkipRequests = [new SkipRequest { LocalPath = null!, DestinationPath = "a.html" }] },
			["skip-requests-destination-path-required"] =
				p => p with { SkipRequests = [new SkipRequest { LocalPath = "a.html", DestinationPath = null! }] },
			["remote-listing-complete"] = p => p with { RemoteListingCompleted = false },
			["source-files-present"] = p => p with { TotalSourceFiles = 0 },
			// 2000 remote files allow 0.2: 401 deletes are over it
			["delete-ratio-within-threshold"] =
				p =>
					p with
					{
						TotalRemoteFiles = 2000,
						DeleteRequests = Deletes(401),
						TotalSyncRequests = p.AddRequests.Count + p.UpdateRequests.Count + 401
					},
			// skipped files are counted into total_sync_requests
			["total-sync-requests-is-add-update-delete"] =
				p => p with { TotalSyncRequests = p.TotalSyncRequests + p.SkipRequests.Count + 1 }
		};
}
