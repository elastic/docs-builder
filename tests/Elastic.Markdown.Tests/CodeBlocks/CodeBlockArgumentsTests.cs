// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Tests.Inline;
using JetBrains.Annotations;

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
	public void Render() => Html.Should().Contain("&lt;1&gt;");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

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
	public void Render() => Html.Should().Contain("<span class=\"code-callout\" data-index=\"1\"></span>");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

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
	public void Render() => Html.Should().Contain("This is a variable");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

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
	public void Render() => Html.Should().Contain("{{a-variable}}");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

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
	public void Render() => Html.Should().Contain("This is a variable").And.Contain("&lt;1&gt;");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
