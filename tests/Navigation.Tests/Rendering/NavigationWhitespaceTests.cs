// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site.Navigation;

namespace Navigation.Tests.Rendering;

public class NavigationWhitespaceTests
{
	[Test]
	public void Collapse_Indentation_BecomesOneLineBreakAndKeepsInlineSpaces() =>
		NavigationWhitespace
			.Collapse(
				"<ul>\n\t\t<li>\n\t\t\t<a href=\"/a\" class=\"sidebar-link\"><span>Run a search</span> <span>GET</span></a>\n\t\t</li>\n\t\t<input\n\t\t\tid=\"x\"\n\t\t\ttype=\"checkbox\">\n</ul>"
			)
			.Should()
			.Be(
				"<ul>\n<li>\n<a href=\"/a\" class=\"sidebar-link\"><span>Run a search</span> <span>GET</span></a>\n</li>\n<input\nid=\"x\"\ntype=\"checkbox\">\n</ul>"
			);
}
