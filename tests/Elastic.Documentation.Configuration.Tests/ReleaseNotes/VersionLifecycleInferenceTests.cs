// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Changelog;

namespace Elastic.Documentation.Configuration.Tests.ReleaseNotes;

public class VersionLifecycleInferenceTests
{
	[Theory]
	[InlineData("9.2.0", "ga")]
	[InlineData("9.2.0-beta.1", "beta")]
	[InlineData("9.2.0-preview.1", "preview")]
	[InlineData("9.2.0-alpha.1", "preview")]
	[InlineData("9.2.0-rc.1", "ga")]
	[InlineData("2026-07-21", "ga")]
	[InlineData("2025-06-01", "ga")]
	public void InferLifecycle_InfersCorrectly(string version, string expected) =>
		VersionLifecycleInference.InferLifecycle(version).Should().Be(expected);
}
