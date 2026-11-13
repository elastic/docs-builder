// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Tests.Inline;
using JetBrains.Annotations;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.CodeBlocks;

public class CodeBlockArgumentsClassTests
{
	[Test]
	public void CanParseCodeBlockArguments()
	{
		const string codeBlockArguments = "callouts=true, subs=false";

		var outcome = CodeBlockArguments.TryParse(codeBlockArguments, out var parsedArgs);

		outcome.Should().BeTrue();
		parsedArgs!.UseSubstitutions.Should().BeFalse();
		parsedArgs.UseCallouts.Should().BeTrue();
	}

	[Test]
	public void CanHandleEmptyAndReturnsDefaultValues()
	{
		const string codeBlockArguments = "";
		var outcome = CodeBlockArguments.TryParse(codeBlockArguments, out var parsedArgs);
		outcome.Should().BeTrue();
		parsedArgs!.UseSubstitutions.Should().BeFalse();
		parsedArgs.UseCallouts.Should().BeTrue();
	}

	[Test]
	public void FailsOnTypo()
	{
		const string codeBlockArguments = "callout=what";
		var result = CodeBlockArguments.TryParse(codeBlockArguments, out _);
		result.Should().BeFalse();
	}

	[Test]
	public void ParsesPartiallyAndUsesDefaultOtherwise()
	{
		const string codeBlockArguments = "callouts=false";
		var outcome = CodeBlockArguments.TryParse(codeBlockArguments, out var parsedArgs);
		outcome.Should().BeTrue();
		parsedArgs!.UseSubstitutions.Should().BeFalse();
		parsedArgs.UseCallouts.Should().BeFalse();
	}
}

[InheritsTests]
public abstract class CodeBlockArgumentsTests(
	string language,
	string arguments,
	[LanguageInjection("csharp")] string code,
	[LanguageInjection("markdown")] string? markdown = null
) : BlockTest<EnhancedCodeBlock>(
	$"""
		 ```{language} {arguments}
		 {code}
		 ```
		 {markdown}
		 """
);

[InheritsTests]
public class DisabledCallouts() : CodeBlockArgumentsTests(
	"csharp",
	"callouts=false",
	"""
	var x = 1; <1>
	var y = x - 2;
	var z = y - 2; <2>
	"""
)
{
	[Test]
	public void Render() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<div class="highlight-csharp notranslate">
				<div class="highlight">
					<pre><code class="language-csharp">var x = 1; &lt;1&gt;
			var y = x - 2;
			var z = y - 2; &lt;2&gt;
			</code>		</pre>
				</div>
			</div>
			""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class EnabledCallouts() : CodeBlockArgumentsTests(
	"csharp",
	"callouts=true",
	"""
	var x = 1; <1>
	""",
	"""
	1. This is a callout
	"""
)
{
	[Test]
	public void Render() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<div class="highlight-csharp notranslate">
				<div class="highlight">
					<pre><code class="language-csharp">var x = 1;<span style="display: inline-block; width: 1ch"></span><span class="code-callout" data-index="1"></span>
			</code>		</pre>
				</div>
			</div>
			<ol class="code-callouts">
				<li>This is a callout</li>
			</ol>
			""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class EnabledSubstitutions() : CodeBlockArgumentsTests(
	"csharp",
	"subs=true",
	"""
	{{a-variable}}
	""",
	"""
	1. This is a callout
	"""
)
{
	[Test]
	public void Render() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<div class="highlight-csharp notranslate">
				<div class="highlight">
					<pre><code class="language-csharp">This is a variable
			</code>		</pre>
				</div>
			</div>
			<ol>
				<li>This is a callout</li>
			</ol>
			""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class DisabledSubstitutions() : CodeBlockArgumentsTests(
	"csharp",
	"subs=false",
	"""
	{{a-variable}}
	""",
	"""
	1. This is a callout
	"""
)
{
	[Test]
	public void Render() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<div class="highlight-csharp notranslate">
				<div class="highlight">
					<pre><code class="language-csharp">{{a-variable}}
			</code>		</pre>
				</div>
			</div>
			<ol>
				<li>This is a callout</li>
			</ol>
			""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}

[InheritsTests]
public class MultipleArguments() : CodeBlockArgumentsTests(
	"csharp",
	"subs=true, callouts=false",
	"""
	{{a-variable}} <1>
	""",
	"""
	1. This is a callout
	"""
)
{
	[Test]
	public void Render() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<div class="highlight-csharp notranslate">
				<div class="highlight">
					<pre><code class="language-csharp">This is a variable &lt;1&gt;
			</code>		</pre>
				</div>
			</div>
			<ol>
				<li>This is a callout</li>
			</ol>
			""",
			DocsHtml.Body()
		);

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
}
