// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.ApiExplorer.Operations;

namespace Elastic.ApiExplorer.Tests;

public class AuthenticationCommonMarkTests
{
	private static AuthScheme Scheme(string label, string example, string? description = null) =>
		new(label.ToLowerInvariant(), label, example, description);

	private static string Write(AuthRequirement requirement)
	{
		var markdown = new StringBuilder();
		OperationCommonMark.WriteRequirement(markdown, requirement, "/api/doc/fixture");
		return markdown.ToString().ReplaceLineEndings("\n");
	}

	[Test]
	public void WriteRequirement_OneScheme_IsAPlainBulletWithItsDescriptionBelow()
	{
		var markdown = Write(new AuthRequirement([Scheme("Basic", "Authorization: Basic <credentials>", "Send the user and password.")]));

		markdown.Should().Be("- Basic: `Authorization: Basic <credentials>`\n  Send the user and password.\n");
	}

	[Test]
	public void WriteRequirement_SeveralSchemes_NestUnderOneBulletThatSaysToSendAll()
	{
		var markdown = Write(new AuthRequirement([
			Scheme("Basic", "Authorization: Basic <credentials>", "Line one.\nLine two."),
			Scheme("Api key", "X-Key: <value>")
		]));

		markdown.Should().Be(
			"- Basic + Api key (send all of these)\n" + "  - Basic: `Authorization: Basic <credentials>`\n" + "    Line one.\n" +
				"    Line two.\n" + "  - Api key: `X-Key: <value>`\n"
		);
	}
}
