// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Journeys.Tests.Targets;
using Nullean.Snapshots;
using Nullean.Snapshots.Http;
using Nullean.Snapshots.TUnit;

namespace Elastic.Documentation.Journeys.Tests.Areas;

/// <summary>
/// The Markdown alternate of a page: <c>&lt;page&gt;.md</c>. The requests use pages of this repository's <c>docs/</c> folder, so only
/// the isolated target runs them.
/// </summary>
[ClassDataSource<JourneyTarget>(Shared = SharedType.PerTestSession)]
public class MarkdownAlternatesHttp(JourneyTarget target)
{
	[Test]
	[HttpFiles("http/markdown-alternates")]
	public async Task Run(SnapshotFile file)
	{
		if (target.Name != "isolated")
			Skip.Test($"The requests use pages of this repository's docs/ folder; the {target.Name} target serves other pages.");

		using var client = new HttpClient { BaseAddress = target.BaseUrl };
		var transcript = await HttpFileSnapshot.RunAsync(
			file.Path,
			new HttpFileSnapshotOptions { Client = client },
			TestContext.Current!.Execution.CancellationToken
		);
		transcript.Should().MatchHttpFileSnapshot(new SnapshotOptions().Scrub(BodyScrubbers.FirstQuotedLine));
	}
}
