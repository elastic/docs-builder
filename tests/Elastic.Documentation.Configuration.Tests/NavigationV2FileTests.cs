// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Configuration.Tests;

public class NavigationV2FileTests
{
	[Fact]
	public void Deserialize_GroupExpanded_ParsesFlag()
	{
		// language=yaml
		var yaml = """
		          nav:
		            - section: Guides
		              url: /docs
		              children:
		                - group: Expanded group
		                  expanded: true
		                  page: docs-content://expanded.md
		                  children:
		                    - page: docs-content://expanded/child.md
		                      title: Child
		                - group: Collapsed group
		                  page: docs-content://collapsed.md
		          """;

		var file = NavigationV2File.Deserialize(yaml);
		var section = file.Nav[0].Should().BeOfType<SectionNavV2Item>().Subject;

		var expandedGroup = section.Children[0].Should().BeOfType<GroupNavV2Item>().Subject;
		expandedGroup.Expanded.Should().BeTrue();

		var collapsedGroup = section.Children[1].Should().BeOfType<GroupNavV2Item>().Subject;
		collapsedGroup.Expanded.Should().BeFalse();
	}
}
