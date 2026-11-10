// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Snapshots;
using Nullean.Snapshots.Http;
using Nullean.Snapshots.TUnit;

namespace Elastic.Documentation.Journeys.Tests.Areas;

/// <summary>
/// The journeys under <c>live/</c> check a published site (prod, edge or demo), whose paths sit under <c>/docs/</c>. Each
/// <c>live</c> folder is claimed here so the area classes do not run it against the isolated site, and the cases show
/// as skipped with a pointer to how to run them. See docs/development/journeys.md#live-journeys.
/// </summary>
public class LiveJourneys
{
	private const string Reason =
		"Live journeys run against a published site: docs-journeys run --url https://<host>/docs/ --journeys tests/Elastic.Documentation.Journeys.Tests/journeys/<area>/live";

	[Test]
	[JourneyFiles("journeys/chrome/live")]
	public void Chrome(SnapshotFile journey) => Skip.Test(Reason);

	[Test]
	[JourneyFiles("journeys/navigation/live")]
	public void Navigation(SnapshotFile journey) => Skip.Test(Reason);

	[Test]
	[JourneyFiles("journeys/versions/live")]
	public void Versions(SnapshotFile journey) => Skip.Test(Reason);

	[Test]
	[JourneyFiles("journeys/api-explorer/live")]
	public void ApiExplorer(SnapshotFile journey) => Skip.Test(Reason);

	[Test]
	[HttpFiles("http/mcp/live")]
	public void Mcp(SnapshotFile file) =>
		Skip.Test(
			"Live HTTP files run against a published site with the {{host}} variable set to its origin, and need the next Nullean.Snapshots release for their @expect lines."
		);
}
