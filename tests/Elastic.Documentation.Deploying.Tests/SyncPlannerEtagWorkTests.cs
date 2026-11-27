// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using DotNet.Globbing;
using Elastic.Documentation.Deploying.Synchronization;

namespace Elastic.Documentation.Deploying.Tests;

public class SyncPlannerEtagWorkTests
{
	/// <summary>
	/// A small deploy against a large bucket reads the ETag only of the included local files that are also remote, as it always did: the listing is
	/// used as it is, with no per-key copy or normalisation of the excluded and remote-only entries.
	/// </summary>
	[Test]
	public void Compute_ReadsRemoteETagsOnlyOfComparedFiles()
	{
		var remote = Enumerable.Range(0, 50_000).ToDictionary(i => $"bucket/{i}.html", i => $"\"{i}\"");
		var asked = new List<string>();
		var excludes = new[] { Glob.Parse("bucket/1*") };
		string[] local = ["bucket/2.html", "bucket/3.html", "bucket/10.html", "new/page.html"];

		var (deletes, adds, updates, skips) = SyncPlanner.Compute(
			local.Select(p => ($"/out/{p}", p)),
			remote,
			e =>
			{
				asked.Add(e);
				return e.Trim('"');
			},
			_ => "local",
			_ => false,
			excludes
		);

		// 2 and 3 are compared; 10 is excluded; new/page.html is not remote
		asked.Should().BeEquivalentTo("\"2\"", "\"3\"");
		adds.Should().ContainSingle().Which.DestinationPath.Should().Be("new/page.html");
		(updates.Count + skips.Count).Should().Be(2);
		deletes.Should().HaveCount(38_889); // every key not excluded: localExists says no
	}
}
