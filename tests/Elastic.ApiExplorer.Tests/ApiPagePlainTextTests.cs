// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Export;

namespace Elastic.ApiExplorer.Tests;

public class ApiPagePlainTextTests
{
	[Test]
	[Arguments("# 1. Authentication", "1. Authentication")]
	[Arguments("# [Core](/docs/api/elasticsearch/v8/group/endpoint-core)", "Core")]
	[Arguments("# The `<tenant>` parameter", "The <tenant> parameter")]
	[Arguments("# Access <tenant>", "Access <tenant>")]
	[Arguments("# Contact <support@example.com>", "Contact support@example.com")]
	public void Read_Heading_IsItsVisibleText(string heading, string expected)
	{
		var text = ApiPagePlainText.Read($"{heading}\n\nBody.\n\n{heading.Replace("# ", "## ")}", "Fallback", description: null);

		text.Title.Should().Be(expected);
		text.Headings.Should().Equal(expected);
	}

	[Test]
	public void Read_QuotedH1BeforeTheTopLevelOne_TakesTheTopLevelTitle() =>
		ApiPagePlainText.Read("> # Note\n\n# Authentication\n\nBody.", "Fallback", description: null).Title.Should().Be("Authentication");

	[Test]
	public void Read_WithoutAnH1_UsesTheFallbackTitle() =>
		ApiPagePlainText.Read("Body only.", "Fallback", description: null).Title.Should().Be("Fallback");

	[Test]
	[Arguments("[Core](/docs/api/elasticsearch/v8/group/endpoint-core) endpoints.", "Core endpoints.")]
	[Arguments("Send a `<ping>` first.", "Send a <ping> first.")]
	[Arguments("Send a <ping> first.", "Send a <ping> first.")]
	[Arguments("<!-- note for authors --> Send a **ping** first.", "Send a ping first.")]
	[Arguments("Uses `max_docs` and `max_age`.", "Uses max_docs and max_age.")]
	[Arguments("Uses _either_ value.", "Uses either value.")]
	public void Description_MarkdownOrPlainText_IsPlainText(string description, string expected) =>
		ApiPagePlainText.Description(description).Should().Be(expected);
}
