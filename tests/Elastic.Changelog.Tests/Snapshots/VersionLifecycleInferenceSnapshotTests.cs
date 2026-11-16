// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Changelog;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Snapshots;

public class VersionLifecycleInferenceSnapshotTests
{
	[Test]
	public void InferLifecycle_TableOfTargets()
	{
		string[] targets =
		[
			"9.2.0",
			"9.2.0-beta.1",
			"9.2.0-BETA",
			"9.2.0-beta",
			"9.2.0-preview.1",
			"9.2.0-alpha.1",
			"9.2.0-rc.1",
			"9.2.0-RC1",
			"9.2.0-SNAPSHOT",
			"9.2.0-x.y",
			"9.2.0-",
			"9.2.0+build.5",
			"9.2.0-beta+build.5",
			"2026-07-21",
			"2025-06-01",
			"2025-6-1",
			"2025-13-45",
			"2025-06",
			"2025-06-01-beta",
			"",
			"latest",
			"not-a-version",
			"v9.2.0",
			"9.2",
			"9",
			"1.0.0-beta.1-extra"
		];
		var lines = targets.Select(t => $"'{t}' => {VersionLifecycleInference.InferLifecycle(t)}");
		string.Join('\n', lines).Should().MatchSnapshot();
	}
}
