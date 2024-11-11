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
---
"""
)
{
	[Fact]
	public void Test1() => File.Title.Should().Be("Elastic Docs v3");
}
