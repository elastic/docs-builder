// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Versions;

namespace Elastic.ApiExplorer.Tests;

public class ApiVersionSwitcherTests
{
	[Fact]
	public void Build_SingleVersion_ReturnsEmpty()
	{
		var items = ApiVersionSwitcher.Build("", "elasticsearch", ["main"], "main");

		items.Should().BeEmpty();
	}

	[Fact]
	public void Build_MultipleVersions_OrdersCurrentMajorFirstAndMarksCurrent()
	{
		var items = ApiVersionSwitcher.Build("", "elasticsearch", ["main", "9", "8"], "8");

		items.Should().HaveCount(3);
		items.Select(i => i.Label).Should().Equal("latest", "v9", "v8");
		items.Select(i => i.Url).Should().Equal("/api/doc/elasticsearch/", "/api/doc/elasticsearch/v9/", "/api/doc/elasticsearch/v8/");
		items.Single(i => i.Selected).Label.Should().Be("v8");
	}

	[Fact]
	public void Build_CurrentMain_LabelsItLatest()
	{
		var items = ApiVersionSwitcher.Build("", "elasticsearch", ["main", "9", "8"], "main");

		items.Select(i => i.Label).Should().Equal("latest", "v9", "v8");
		items.Single(i => i.Selected).Label.Should().Be("latest");
	}

	[Fact]
	public void CurrentVersionLabel_PrefersProductVersioningBase()
	{
		var items = ApiVersionSwitcher.Build("", "elasticsearch", ["main", "8"], "main");

		var label = ApiVersionSwitcher.CurrentVersionLabel(StackVersioning(), items);

		label.Should().Be("9.0+");
	}

	[Fact]
	public void SerializeDropdownItems_EmitsDocsDropdownShape()
	{
		var items = ApiVersionSwitcher.Build("", "elasticsearch", ["main", "9", "8"], "main");

		var json = ApiVersionSwitcher.SerializeDropdownItems(items);

		json.Should().Contain("\"name\":\"latest\"");
		json.Should().Contain("\"name\":\"v9\"");
		json.Should().Contain("\"href\":\"/api/doc/elasticsearch/\"");
		json.Should().Contain("\"href\":\"/api/doc/elasticsearch/v9/\"");
		json.Should().Contain("\"disabled\":false");
	}

	private static VersioningSystem StackVersioning() =>
		new() { Id = VersioningSystemId.Stack, Base = new SemVersion(9, 0, 0), Current = new SemVersion(9, 5, 4) };
}
