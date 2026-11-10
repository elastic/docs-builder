// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Journeys.Tests.Targets;
using Nullean.Snapshots;
using Nullean.Snapshots.Browser;
using Nullean.Snapshots.TUnit;

namespace Elastic.Documentation.Journeys.Tests.Areas;

[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
public class ChromeJourneys(JourneyTarget target)
{
	[Test]
	[JourneyFiles("journeys/chrome")]
	public async Task Run(SnapshotFile journey)
	{
		var options = target.OptionsFor(journey);
		await JourneyTest.RunAsync(journey, options).ConfigureAwait(false);
	}
}
