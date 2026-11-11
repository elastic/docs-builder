// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Snapshots;
using Nullean.Snapshots.Http;
using Nullean.Snapshots.TUnit;

namespace Elastic.Documentation.Journeys.Tests.Areas;

/// <summary>
/// The journeys under <c>synth/</c> check a published site (prod, edge or demo), whose paths sit under <c>/docs/</c>. Each
/// <c>synth</c> folder is claimed here so the area classes do not run it against the isolated site, and the cases show
/// as skipped with a pointer to `mise run journeys:synth`. See docs/development/journeys.md#synthetics-journeys.
/// </summary>
public class SynthJourneys
{
	private const string Reason =
		"Synthetics journey: it checks a published site, not the isolated one this test serves. Run it with `mise run journeys:synth <prod|edge|demo>`, see docs/development/journeys.md#synthetics-journeys";

	[Test]
	[JourneyFiles("journeys/chrome/synth")]
	public void Chrome(SnapshotFile journey) => Skip.Test(Reason);

	[Test]
	[JourneyFiles("journeys/navigation/synth")]
	public void Navigation(SnapshotFile journey) => Skip.Test(Reason);

	[Test]
	[JourneyFiles("journeys/versions/synth")]
	public void Versions(SnapshotFile journey) => Skip.Test(Reason);

	[Test]
	[JourneyFiles("journeys/api-explorer/synth")]
	public void ApiExplorer(SnapshotFile journey) => Skip.Test(Reason);

	[Test]
	[HttpFiles("http/mcp/synth")]
	public void Mcp(SnapshotFile file) =>
		Skip.Test(
			"Synthetics HTTP files run against a published site with the {{host}} variable set to its origin, and need the next Nullean.Snapshots release for their @expect lines."
		);

	[Test]
	[JourneyFiles("journeys/compare")]
	public void Compare(SnapshotFile journey) =>
		Skip.Test(
			"Compare journey: it records what two published sites show and compares them. Run it with `mise run journeys:compare`, see docs/development/journeys.md#compare-two-deployed-sites"
		);
}
