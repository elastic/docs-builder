// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Tests.Directives;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

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
	public void IncludesInclusionHtml() => Html.Should().MatchInlineHtmlSnapshot("<p><em>Hello world</em></p>", DocsHtml.Body());
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
	public void EmitsError() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("error {repo}/docs/index.md:3 `{repo}/docs/_snippets/notfound.md` does not exist.");
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
	public void EmitsError() =>
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("error {repo}/docs/index.md:3 include requires an argument.");
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
	public void EmitsError() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
		warning {repo}/docs/test.md Document has no title, using file name as title.
		error {repo}/docs/index.md:3 {include} only supports including snippets from `_snippet` folders. `{repo}/docs/test.md` is not a snippet
		"""
			);
}

[InheritsTests]
public class IncludeRelativeTraversalBlocked() : DirectiveTest<IncludeBlock>("""
:::{include} ../../../outside.txt
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) => fileSystem.AddFile(@"outside.txt", "some content");

	[Test]
	public void EmitsError() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("error {repo}/docs/index.md:3 Include path must resolve within the documentation source directory.");
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
	public void EmitsError() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error {repo}/docs/_snippets/test.md:1 {include} cyclical include detected `{repo}/docs/_snippets/test.md` points to itself"
			);
}
