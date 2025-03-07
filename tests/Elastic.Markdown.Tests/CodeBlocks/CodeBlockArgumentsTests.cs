// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information


using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Tests.Inline;
using FluentAssertions;
using JetBrains.Annotations;

namespace Elastic.Markdown.Tests.CodeBlocks;

public class CodeBlockArgumentsClassTests
{
	[Fact]
	public void CanParseCodeBlockArguments()
	{
		const string codeBlockArguments = "callouts=true, subs=false";

		var outcome = CodeBlockArguments.TryParse(codeBlockArguments, out var parsedArgs);

		outcome.Should().BeTrue();
		parsedArgs!.IsSubstitutionsEnabled.Should().BeFalse();
		parsedArgs.IsCalloutsEnabled.Should().BeTrue();
	}

	[Fact]
	public void CanHandleEmptyAndReturnsDefaultValues()
	{
		const string codeBlockArguments = "";
		var outcome = CodeBlockArguments.TryParse(codeBlockArguments, out var parsedArgs);
		outcome.Should().BeTrue();
		parsedArgs!.IsSubstitutionsEnabled.Should().BeFalse();
		parsedArgs.IsCalloutsEnabled.Should().BeTrue();
	}

	[Fact]
	public void FailsOnTypo()
	{
		const string codeBlockArguments = "callout=what";
		var result = CodeBlockArguments.TryParse(codeBlockArguments, out _);
		result.Should().BeFalse();
	}

	[Fact]
	public void ParsesPartiallyAndUsesDefaultOtherwise()
	{
		const string codeBlockArguments = "callouts=false";
		var outcome = CodeBlockArguments.TryParse(codeBlockArguments, out var parsedArgs);
		outcome.Should().BeTrue();
		parsedArgs!.IsSubstitutionsEnabled.Should().BeFalse();
		parsedArgs.IsCalloutsEnabled.Should().BeFalse();
	}
}


public abstract class CodeBlockArgumentsTests(
	ITestOutputHelper output,
	string language,
	string arguments,
	[LanguageInjection("csharp")] string code,
	[LanguageInjection("markdown")] string? markdown = null
)
	: BlockTest<EnhancedCodeBlock>(output,
		$"""
		 ```{language} {arguments}
		 {code}
		 ```
		 {markdown}
		 """
	);

public class DisabledCallouts(ITestOutputHelper output) : CodeBlockArgumentsTests(output, "csharp", "callouts=false",
	"""
	var x = 1; <1>
	var y = x - 2;
	var z = y - 2; <2>
	"""
)
{
	[Fact]
	public void Render() => Html.Should().Contain("&lt;1&gt;");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class EnabledCallouts(ITestOutputHelper output) : CodeBlockArgumentsTests(output, "csharp", "callouts=true",
	"""
	var x = 1; <1>
	""",
	"""
	1. This is a callout
	"""
)
{
	[Fact]
	public void Render() => Html.Should().Contain("<span class=\"code-callout\" data-index=\"1\">1</span>");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class EnabledSubstitutions(ITestOutputHelper output) : CodeBlockArgumentsTests(output, "csharp", "subs=true",
	"""
	{{a-variable}}
	""",
	"""
	1. This is a callout
	"""
)
{
	[Fact]
	public void Render() => Html.Should().Contain("This is a variable");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}


public class DisabledSubstitutions(ITestOutputHelper output) : CodeBlockArgumentsTests(output, "csharp", "subs=false",
	"""
	{{a-variable}}
	""",
	"""
	1. This is a callout
	"""
)
{
	[Fact]
	public void Render() => Html.Should().Contain("{{a-variable}}");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class MultipleArguments(ITestOutputHelper output) : CodeBlockArgumentsTests(output, "csharp", "subs=true, callouts=false",
	"""
	{{a-variable}} <1>
	""",
	"""
	1. This is a callout
	"""
)
{
	[Fact]
	public void Render() => Html
		.Should().Contain("This is a variable")
		.And.Contain("&lt;1&gt;");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
