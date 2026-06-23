// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Markdown.Extensions.CliReference;

namespace Elastic.Markdown.Tests.CliReference;

public class CliSupplementalDocTests
{
	[Fact]
	public void RootPage_PreservesFrontMatterAsMetadata()
	{
		var schema = CreateSchema();
		const string raw = """
			---
			description: Use the Elastic CLI from the command line.
			applies_to:
			  stack: preview
			---
			""";

		var supplemental = CliSupplementalDoc.Parse(raw);
		var markdown = CliMarkdownGenerator.RootPage(schema, supplemental).ReplaceLineEndings("\n");

		var expectedStart = """
			---
			description: Use the Elastic CLI from the command line.
			applies_to:
			  stack: preview
			---

			# elastic
			""".ReplaceLineEndings("\n");

		markdown.Should().StartWith(expectedStart);
		markdown.Should().NotContain("description: Use the Elastic CLI from the command line.\n\n");
	}

	[Fact]
	public void RootPage_StripsFrontMatterBeforeParsingDescription()
	{
		var schema = CreateSchema();
		const string raw = """
			---
			description: Metadata description.
			---

			User-facing supplemental description.
			""";

		var supplemental = CliSupplementalDoc.Parse(raw);
		var markdown = CliMarkdownGenerator.RootPage(schema, supplemental).ReplaceLineEndings("\n");

		markdown.Should().Contain("\n# elastic\n\nUser-facing supplemental description.\n");
		markdown.Should().NotContain("\nMetadata description.\n");
	}

	private static CliSchema CreateSchema() => new(
		SchemaVersion: 1,
		Name: "elastic",
		Description: "Schema description.",
		GlobalOptions: [],
		RootDefault: null,
		Commands: [],
		Namespaces: []
	);
}
