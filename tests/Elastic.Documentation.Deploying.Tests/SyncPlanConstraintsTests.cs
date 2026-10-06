// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Deploying.Synchronization;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Deploying.Tests;

public class SyncPlanConstraintsTests
{
	[Test]
	public void Laws() => SyncPlanConstraints.All.Laws(Valid(), CounterExamples);

	private static IReadOnlyList<DeleteRequest> Deletes(int count) =>
		[.. Enumerable.Range(0, count).Select(i => new DeleteRequest { DestinationPath = $"old/{i}.html" })];

	private static SyncPlan Valid() =>
		new()
		{
			DeleteThresholdDefault = null,
			ExcludePatterns = [],
			RemoteListingCompleted = true,
			TotalSourceFiles = 2000,
			TotalRemoteFiles = 2000,
			TotalSyncRequests = 1,
			DeleteRequests = [],
			AddRequests = [new AddRequest { LocalPath = "/out/a.html", DestinationPath = "a.html" }],
			UpdateRequests = [],
			SkipRequests = []
		};

	private static IReadOnlyDictionary<string, Func<SyncPlan, SyncPlan>> CounterExamples =>
		new Dictionary<string, Func<SyncPlan, SyncPlan>>
		{
			["remote-listing-complete"] = p => p with { RemoteListingCompleted = false },
			["source-files-present"] = p => p with { TotalSourceFiles = 0 },
			// 2000 remote files allow 0.2: 401 deletes are over it
			["delete-ratio-within-threshold"] = p => p with { DeleteRequests = Deletes(401), TotalSyncRequests = 402 },
			// skipped files are counted into total_sync_requests
			["total-sync-requests-is-add-update-delete"] =
				p =>
					p with
					{
						SkipRequests = [new SkipRequest { LocalPath = "/out/b.html", DestinationPath = "b.html" }],
						TotalSyncRequests = 2
					}
		};
}
