// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Tests.Directives;

namespace Elastic.Markdown.Tests.FileInclusion;

[InheritsTests]
public class IncludeTests() : DirectiveTest<IncludeBlock>("""
:::{include} _snippets/test.md
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = "*Hello world*";
		fileSystem.AddFile(@"docs/_snippets/test.md", inclusion);
	}

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void IncludesInclusionHtml() => Html.ShouldBeHtml("<p><em>Hello world</em></p>");
}

[InheritsTests]
public class IncludeSubstitutionTests() : DirectiveTest<IncludeBlock>("""
---
sub:
  foo: "bar"
---
:::{include} _snippets/test.md
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = "*Hello {{foo}}*";
		fileSystem.AddFile(@"docs/_snippets/test.md", inclusion);
	}

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void InclusionInheritsYamlContext() => Html.Should().Contain("Hello bar").And.Be("<p><em>Hello bar</em></p>");
}

[InheritsTests]
public class IncludeNotFoundTests() : DirectiveTest<IncludeBlock>("""
:::{include} _snippets/notfound.md
:::
""")
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void IncludesNothing() => Html.Should().Be("");

	[Test]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty().And.HaveCount(1);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Error);
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("notfound.md` does not exist"));
	}
}

[InheritsTests]
public class IncludeRequiresArgument() : DirectiveTest<IncludeBlock>("""
:::{include}
:::
""")
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void IncludesNothing() => Html.Should().Be("");

	[Test]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty().And.HaveCount(1);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Error);
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("include requires an argument."));
	}
}

[InheritsTests]
public class IncludeNeedsToLiveInSpecialFolder() : DirectiveTest<IncludeBlock>("""
```{include} test.md
```
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = "*Hello world*";
		fileSystem.AddFile(@"docs/test.md", inclusion);
	}

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void IncludesNothing() => Html.Should().Be("");

	[Test]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("only supports including snippets from `_snippet` folders."));
	}
}

[InheritsTests]
public class IncludeRelativeTraversalBlocked() : DirectiveTest<IncludeBlock>("""
:::{include} ../../../outside.txt
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) => fileSystem.AddFile(@"outside.txt", "some content");

	[Test]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("must resolve within the documentation source directory"));
	}
}

[InheritsTests]
public class CanNotIncludeItself() : DirectiveTest<IncludeBlock>("""
```{include} _snippets/test.md
```
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
:::{include} test.md
:::
""";
		fileSystem.AddFile(@"docs/_snippets/test.md", inclusion);
	}

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void IncludesNothing() => Html.Should().Be("");

	[Test]
	public void EmitsError()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty().And.HaveCount(1);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Error);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("cyclical include detected"));
	}
}
