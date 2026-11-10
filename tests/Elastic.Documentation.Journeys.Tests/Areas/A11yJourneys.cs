// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Journeys.Tests.Targets;
using Nullean.Snapshots;
using Nullean.Snapshots.Browser;
using Nullean.Snapshots.TUnit;

namespace Elastic.Documentation.Journeys.Tests.Areas;

[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
public class A11yJourneys(JourneyTarget target)
{
	[Test]
	[JourneyFiles("journeys/a11y")]
	public async Task Run(SnapshotFile journey)
	{
		// color-contrast depends on how the platform renders text: macOS flags the feedback buttons and Linux does not, so
		// each platform has its own verified transcript.
		var options = await target.OptionsForIsolatedJourneysAsync(OperatingSystem.IsMacOS() ? "macos" : "linux").ConfigureAwait(false);
		await JourneyTest.RunAsync(journey, options).ConfigureAwait(false);
	}
}
