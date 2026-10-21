// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiVersionSwitcherTests
{
	[Test]
	public void Build_SingleVersion_ReturnsEmpty()
	{
		var items = ApiVersionSwitcher.Build("", "elasticsearch", ["main"], "main");

		items.Should().BeEmpty();
	}

	[Test]
	public void Build_MultipleVersions_OrdersCurrentMajorFirstAndMarksCurrent()
	{
		var items = ApiVersionSwitcher.Build("", "elasticsearch", ["main", "9", "8"], "8");

		items.Should().HaveCount(3);
		items.Select(i => i.Label).Should().Equal("main", "v9", "v8");
		items.Select(i => i.Url).Should().Equal("/api/doc/elasticsearch", "/api/doc/elasticsearch/v9", "/api/doc/elasticsearch/v8");
		items.Single(i => i.Selected).Label.Should().Be("v8");
	}

	[Test]
	public void Build_CurrentMain_LabelsItLatest()
	{
		var items = ApiVersionSwitcher.Build("", "elasticsearch", ["main", "9", "8"], "main");

		items.Select(i => i.Label).Should().Equal("main", "v9", "v8");
		items.Single(i => i.Selected).Label.Should().Be("main");
	}
}
