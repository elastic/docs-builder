// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public class YamlFrontMatterTests(ITestOutputHelper output) : DirectiveTest(output,
"""
---
title: Elastic Docs v3
navigation_title: "Documentation Guide"
sub:
  key: "value"
---
"""
)
{
	[Fact]
	public void ReadsTitle() => File.Title.Should().Be("Elastic Docs v3");

	[Fact]
	public void ReadsNavigationTitle() => File.NavigationTitle.Should().Be("Documentation Guide");

	[Fact]
	public void ReadsSubstitutions()
	{
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter!.Properties.Should().NotBeEmpty()
			.And.HaveCount(1)
			.And.ContainKey("key");
	}
}
