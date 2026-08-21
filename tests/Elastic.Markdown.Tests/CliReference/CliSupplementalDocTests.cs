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
	public void RootPage_DoesNotEmitFrontMatter()
	{
		// Generator no longer owns frontmatter — BuildMarkdown() in CliRootFile/CliNamespaceFile/
		// CliCommandFile prepends it. Verify the generator starts at the heading.
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

		markdown.Should().StartWith("# elastic");
		markdown.Should().NotContain("---");
	}

	[Fact]
	public void SupplementalDoc_ExtractsFrontMatterForBuildMarkdown()
	{
		// CliRootFile.BuildMarkdown() prepends supplemental.FrontMatter before the generated body.
		// Verify the property is correctly populated so the prepend produces valid YAML frontmatter.
		const string raw = """
			---
			description: Use the Elastic CLI from the command line.
			applies_to:
			  stack: preview
			---
			""";

		var supplemental = CliSupplementalDoc.Parse(raw);

		supplemental.Should().NotBeNull();
		supplemental!.FrontMatter.Should().NotBeNullOrWhiteSpace();
		supplemental.FrontMatter.Should().Contain("applies_to:");
		supplemental.FrontMatter.Should().Contain("stack: preview");
	}

	[Fact]
	public void SupplementalDoc_FrontMatterPrependProducesValidMarkdown()
	{
		// Simulates what BuildMarkdown() does: prepend FrontMatter before the generated body.
		// The combined string must start with the YAML block, immediately followed by the heading.
		var schema = CreateSchema();
		const string raw = """
			---
			applies_to:
			  stack: preview
			---
			""";

		var supplemental = CliSupplementalDoc.Parse(raw);
		var body = CliMarkdownGenerator.RootPage(schema, supplemental).ReplaceLineEndings("\n");
		var combined = $"{supplemental!.FrontMatter}\n\n{body}";

		combined.Should().StartWith("---\n");
		combined.Should().Contain("---\n\n# elastic");
		// Frontmatter must not appear twice: opening + closing delimiter = exactly 2 occurrences
		(combined.Split("---").Length - 1).Should().Be(2);
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
