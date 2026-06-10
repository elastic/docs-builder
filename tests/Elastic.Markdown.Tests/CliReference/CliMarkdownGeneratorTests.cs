// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Markdown.Extensions.CliReference;

namespace Elastic.Markdown.Tests.CliReference;

public class CliMarkdownGeneratorTests
{
	[Fact]
	public void RootPage_UsesTitleOverrideForHeading()
	{
		var schema = new CliSchema(
			SchemaVersion: 1,
			Name: "elastic",
			Description: "Interact with Elastic from the command line.",
			GlobalOptions: [],
			RootDefault: null,
			Commands: [],
			Namespaces: []
		);

		var markdown = CliMarkdownGenerator.RootPage(schema, null, "Elastic CLI reference");

		markdown.Should().StartWith("# Elastic CLI reference");
		markdown.Should().Contain("Interact with Elastic from the command line.");
	}
}
