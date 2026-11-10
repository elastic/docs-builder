// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Journeys.Tests.Targets;
using Nullean.Snapshots;
using Nullean.Snapshots.Browser;
using Nullean.Snapshots.TUnit;

namespace Elastic.Documentation.Journeys.Tests.Areas;

[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
public class LandingJourneys(JourneyTarget target)
{
	[Test]
	[JourneyFiles("journeys/landing")]
	public Task Run(SnapshotFile journey) => JourneyTest.RunAsync(journey, target.Options);
}
