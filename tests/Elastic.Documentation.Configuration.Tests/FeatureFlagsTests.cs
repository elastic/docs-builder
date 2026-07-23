// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Builder;

namespace Elastic.Documentation.Configuration.Tests;

public class FeatureFlagsTests
{
	[Fact]
	public void StaticSearchEnabled_NotConfigured_ReturnsFalse()
	{
		var flags = new FeatureFlags([]);

		flags.StaticSearchEnabled.Should().BeFalse();
	}

	[Fact]
	public void StaticSearchEnabled_Configured_ReturnsValue()
	{
		var flags = new FeatureFlags(new Dictionary<string, bool>
		{
			["static-search"] = true
		});

		flags.StaticSearchEnabled.Should().BeTrue();
	}
}
