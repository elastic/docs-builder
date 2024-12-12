// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Tests.Directives;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.FrontMatter;

public class ProductConstraintTests(ITestOutputHelper output) : DirectiveTest(output,
"""
---
title: Elastic Docs v3
navigation_title: "Documentation Guide"
applies:
  self:
    stack: 7.7
  cloud:
    serverless: 1.0.0
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
		var appliesTo = File.YamlFrontMatter!.AppliesTo;
		appliesTo.Should().NotBeNull();
		appliesTo!.SelfManaged.Should().NotBeNull();
		appliesTo.Cloud.Should().NotBeNull();
		appliesTo.Cloud!.Serverless.Should().BeEquivalentTo(new SemVersion(1,0,0));
	}
}

public class CanSpecifyAllForProductVersions(ITestOutputHelper output) : DirectiveTest(output,
"""
---
title: Elastic Docs v3
navigation_title: "Documentation Guide"
applies:
  self:
    stack: all
---
"""
)
{
	[Fact]
	public void Assert() =>
		File.YamlFrontMatter!.AppliesTo!.SelfManaged!.Stack.Should().BeEquivalentTo(AllVersions.Instance);
}

public class EmptyProductVersionMeansAll(ITestOutputHelper output) : DirectiveTest(output,
"""
---
title: Elastic Docs v3
navigation_title: "Documentation Guide"
applies:
  self:
    stack:
---
"""
)
{
	[Fact]
	public void Assert() =>
		File.YamlFrontMatter!.AppliesTo!.SelfManaged!.Stack.Should().BeEquivalentTo(AllVersions.Instance);
}

public class EmptyCloudSetsAllProductsToAll(ITestOutputHelper output) : DirectiveTest(output,
"""
---
title: Elastic Docs v3
navigation_title: "Documentation Guide"
applies:
  cloud:
---
"""
)
{
	[Fact]
	public void Assert() =>
		File.YamlFrontMatter!.AppliesTo!.Cloud!.Ess.Should().BeEquivalentTo(AllVersions.Instance);
}
