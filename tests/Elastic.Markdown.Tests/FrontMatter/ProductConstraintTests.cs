// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.FrontMatter;
using Elastic.Markdown.Tests.Directives;
using FluentAssertions;
using static Elastic.Markdown.Myst.FrontMatter.ProductLifecycle;

//TODO remove 'applies', Deployment and associated types when all content sets have moved over to `apply`
#pragma warning disable CS0618 // Type or member is obsolete

namespace Elastic.Markdown.Tests.FrontMatter;

public class ProductConstraintTests(ITestOutputHelper output) : DirectiveTest(output,
"""
---
navigation_title: "Documentation Guide"
applies:
  stack: ga 8.1
  serverless: tech-preview
  hosted: beta 8.1.1
  eck: beta 3.0.2
  ece: unavailable
---
"""
)
{
	[Fact]
	public void Assert()
	{
		File.YamlFrontMatter.Should().NotBeNull();
		var appliesTo = File.YamlFrontMatter!.AppliesTo;
		appliesTo.Should().NotBeNull();
		appliesTo!.Cloud.Should().NotBeNull();
		appliesTo.Cloud!.Serverless.Should().BeEquivalentTo(new Applicability { Lifecycle = TechnicalPreview });
		appliesTo.Cloud!.Hosted.Should().BeEquivalentTo(new Applicability { Lifecycle = Beta, Version = new(8, 1, 1) });
		appliesTo.SelfManaged.Should().NotBeNull();
		appliesTo.SelfManaged!.Eck.Should().BeEquivalentTo(new Applicability { Lifecycle = Beta, Version = new(3, 0, 2) });
		appliesTo.SelfManaged!.Ece.Should().BeEquivalentTo(new Applicability { Lifecycle = Unavailable });
		appliesTo.SelfManaged!.Stack.Should().BeEquivalentTo(new Applicability { Lifecycle = GenerallyAvailable, Version = new(8, 1, 0) });
	}
}

public abstract class ParsingTests(ITestOutputHelper output, string moniker, Applicability? expected)
	: DirectiveTest(output,
$"""
---
navigation_title: "Documentation Guide"
applies:
  serverless: {moniker}
---
"""
)
{
	[Fact]
	public void Assert()
	{
		File.YamlFrontMatter.Should().NotBeNull();
		var appliesTo = File.YamlFrontMatter!.AppliesTo;
		appliesTo.Should().NotBeNull();
		appliesTo!.Cloud.Should().NotBeNull();
		appliesTo.Cloud!.Serverless.Should().BeEquivalentTo(expected);
	}
}

public class ParsesGa(ITestOutputHelper output) : ParsingTests(output, "ga", new() { Lifecycle = GenerallyAvailable });
public class ParsesDev(ITestOutputHelper output) : ParsingTests(output, "dev", new() { Lifecycle = Development });
public class ParsesDevelopment(ITestOutputHelper output) : ParsingTests(output, "development", new() { Lifecycle = Development });
public class ParsesBeta(ITestOutputHelper output) : ParsingTests(output, "beta", new() { Lifecycle = Beta });
public class ParsesComing(ITestOutputHelper output) : ParsingTests(output, "coming", new() { Lifecycle = Coming });
public class ParsesDeprecated(ITestOutputHelper output) : ParsingTests(output, "deprecated", new() { Lifecycle = Deprecated });
public class ParsesDiscontinued(ITestOutputHelper output) : ParsingTests(output, "discontinued", new() { Lifecycle = Discontinued });
public class ParsesUnavailable(ITestOutputHelper output) : ParsingTests(output, "unavailable", new() { Lifecycle = Unavailable });
public class ParsesTechnicalPreview(ITestOutputHelper output) : ParsingTests(output, "tech-preview", new() { Lifecycle = TechnicalPreview });
public class ParsesPreview(ITestOutputHelper output) : ParsingTests(output, "preview", new() { Lifecycle = TechnicalPreview });
public class ParsesEmpty(ITestOutputHelper output) : ParsingTests(output, "", Applicability.GenerallyAvailable);
public class ParsesAll(ITestOutputHelper output) : ParsingTests(output, "all", Applicability.GenerallyAvailable);
public class ParsesWithVersion(ITestOutputHelper output) : ParsingTests(output, "ga 7.7.0", new() { Lifecycle = GenerallyAvailable, Version = new(7, 7, 0) });
public class ParsesWithAllVersion(ITestOutputHelper output) : ParsingTests(output, "ga all", new() { Lifecycle = GenerallyAvailable, Version = AllVersions.Instance });

public class CanSpecifyAllForProductVersions(ITestOutputHelper output) : DirectiveTest(output,
"""
---
navigation_title: "Documentation Guide"
applies:
  stack: all
---
"""
)
{
	[Fact]
	public void Assert() =>
		File.YamlFrontMatter!.AppliesTo!.SelfManaged!.Stack.Should().BeEquivalentTo(Applicability.GenerallyAvailable);
}

public class EmptyProductVersionMeansAll(ITestOutputHelper output) : DirectiveTest(output,
"""
---
navigation_title: "Documentation Guide"
applies:
  stack:
---
"""
)
{
	[Fact]
	public void Assert() =>
		File.YamlFrontMatter!.AppliesTo!.SelfManaged!.Stack.Should().BeEquivalentTo(Applicability.GenerallyAvailable);
}

public class EmptyCloudSetsAllCloudProductsToAll(ITestOutputHelper output) : DirectiveTest(output,
"""
---
navigation_title: "Documentation Guide"
applies:
  cloud:
---
"""
)
{
	[Fact]
	public void Assert() =>
		File.YamlFrontMatter!.AppliesTo!.Cloud!.Hosted.Should().BeEquivalentTo(Applicability.GenerallyAvailable);
}

public class EmptySelfSetsAllSelfManagedProductsToAll(ITestOutputHelper output) : DirectiveTest(output,
"""
---
navigation_title: "Documentation Guide"
applies:
  self:
  stack: deprecated 9.0.0
---
"""
)
{
	[Fact]
	public void Assert()
	{
		File.YamlFrontMatter!.AppliesTo!.SelfManaged!.Eck.Should()
			.BeEquivalentTo(Applicability.GenerallyAvailable);
		File.YamlFrontMatter!.AppliesTo!.SelfManaged!.Stack.Should()
			.BeEquivalentTo(new Applicability { Lifecycle = Deprecated, Version = new(9, 0, 0) });
	}
}

public class CloudProductsOverwriteDeploymentType(ITestOutputHelper output) : DirectiveTest(output,
"""
---
navigation_title: "Documentation Guide"
applies:
  cloud:
---
"""
)
{
	[Fact]
	public void Assert() =>
		File.YamlFrontMatter!.AppliesTo!.Cloud!.Hosted.Should().BeEquivalentTo(Applicability.GenerallyAvailable);
}

