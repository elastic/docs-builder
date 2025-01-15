// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Interpolation;

public class InterpolationTests
{
	[Fact]
	public void ReplacesVariables()
	{
		var span = "My text {{with-variables}} {{not-defined}}".AsSpan();
		var replacements = new Dictionary<string, string> { { "with-variables", "With Variables" } };
		var replaced = span.ReplaceSubstitutions(replacements, out var replacement);

		replaced.Should().BeTrue();
		replacement.Should().Be("My text With Variables {{not-defined}}");
	}

	[Fact]
	public void OnlyReplacesDefinedVariables()
	{
		var span = "My text {{not-defined}}".AsSpan();
		var replacements = new Dictionary<string, string> { { "with-variables", "With Variables" } };
		var replaced = span.ReplaceSubstitutions(replacements, out var replacement);

		replaced.Should().BeFalse();
		// no need to allocate replacement we can continue with span
		replacement.Should().BeNull();
	}
}
