// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Build.Tests;

public class PublishedJsonPathTests
{
	private static readonly NavigationTocMapping[] AutoOpsMappings =
	[
		new() { Source = new Uri("docs-content://reference/autoops-insights/"), SourcePathPrefix = "reference/autoops-insights" },
		new() { Source = new Uri("autoops-insights://elasticsearch/"), SourcePathPrefix = "reference/autoops-insights/elasticsearch" }
	];

	[Test]
	public void NestedJsonUsesTheNavigationMount()
	{
		var mappings = AutoOpsMappings.ToDictionary(mapping => mapping.Source);
		var mapped = GlobalNavigationPathProvider.TryGetMountedRelativePath(
			"autoops-insights",
			"elasticsearch/catalog.json",
			mappings,
			out var prefix,
			out var remainder
		);

		mapped.Should().BeTrue();
		prefix.Should().Be("reference/autoops-insights/elasticsearch");
		remainder.Should().Be("catalog.json");
	}

	[Test]
	public void RootJsonUsesTheDocsetMount()
	{
		var mappings = AutoOpsMappings.ToDictionary(mapping => mapping.Source);
		GlobalNavigationPathProvider
			.TryGetMountedRelativePath("autoops-insights", "autoops-insights.json", mappings, out _, out _)
			.Should()
			.BeFalse();

		GlobalNavigationPathProvider.DocsetRootPrefix("autoops-insights", AutoOpsMappings).Should().Be("reference/autoops-insights");
	}

	[Test]
	public void SingleMountKeepsItsOwnPrefix()
	{
		NavigationTocMapping[] mappings = [new() { Source = new Uri("search-ui://reference/"), SourcePathPrefix = "reference/search-ui" }];

		GlobalNavigationPathProvider.DocsetRootPrefix("search-ui", mappings).Should().Be("reference/search-ui");
	}
}
