// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Inline.Substitutions;

public class ReadSubFromYamlFrontmatter : DocumentTest
{
	protected override string Document =>
		"""
		---
		sub:
		  hello-world: "Hello World!"
		---
		The following should be subbed: {{hello-world}}
		not a comment
		""";

	[Fact(DisplayName = "validate HTML: replace substitution")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml("""
		<p>The following should be subbed: Hello World!
		not a comment</p>
		""");
}

public class RequiresValidSyntaxAndKeyToBeFound : DocumentTest
{
	protected override string Document =>
		"""
		---
		sub:
		  hello-world: "Hello World!"
		---
		# Testing substitutions

		The following should be subbed: {{hello-world}}
		not a comment
		not a {{valid-key}}
		not a {substitution}
		The following should be subbed too: {{ hello-world }}
		""";

	[Fact(DisplayName = "emits an error when sub key is not found")]
	public async Task EmitsError() => await Docs.HasError("key {valid-key} is undefined");

	[Fact(DisplayName = "validate HTML: leaves non subs alone")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p>The following should be subbed: Hello World!
			not a comment
			not a {{valid-key}}
			not a {substitution}
		   The following should be subbed too: Hello World!
			</p>
		"""
		);
}
