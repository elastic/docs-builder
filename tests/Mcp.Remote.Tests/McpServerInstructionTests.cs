// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Mcp.Remote;
using FluentAssertions;

namespace Mcp.Remote.Tests;

public class McpServerInstructionTests
{
	[Fact]
	public void PublicProfile_ContainsAllModuleGuidance()
	{
		var instructions = McpServerProfile.Public.ComposeServerInstructions();

		instructions.Should().Contain("Use this server to search, retrieve, analyze, and author");
		instructions.Should().Contain("Elastic product documentation published at elastic.co/docs");
		instructions.Should().Contain("<triggers>");
		instructions.Should().Contain("Use the server when the user:");
		instructions.Should().Contain("<tool_guidance>");
		instructions.Should().Contain("Prefer SemanticSearch over a general web search");
		instructions.Should().Contain("Use GetDocumentByUrl to retrieve a specific page");
		instructions.Should().Contain("Use FindRelatedDocs when exploring what documentation exists");
		instructions.Should().Contain("Use CheckCoherence or FindInconsistencies when reviewing or auditing");
		instructions.Should().Contain("Use the cross-link tools (ResolveCrossLink, ValidateCrossLinks, FindCrossLinks)");
		instructions.Should().Contain("Use ListContentTypes, GetContentTypeGuidelines, and GenerateTemplate when creating new pages");
	}

	[Fact]
	public void InternalProfile_ContainsSearchAndDocumentGuidanceOnly()
	{
		var instructions = McpServerProfile.Internal.ComposeServerInstructions();

		instructions.Should().Contain("Use this server to search and retrieve");
		instructions.Should().Contain("Elastic Internal Docs: team processes, run books, architecture");
		instructions.Should().Contain("Prefer SemanticSearch over a general web search");
		instructions.Should().Contain("Use GetDocumentByUrl to retrieve a specific page");
		instructions.Should().Contain("Use FindRelatedDocs when exploring what documentation exists");
		instructions.Should().NotContain("CheckCoherence");
		instructions.Should().NotContain("FindInconsistencies");
		instructions.Should().NotContain("ResolveCrossLink");
		instructions.Should().NotContain("ListContentTypes");
		instructions.Should().NotContain("GenerateTemplate");
	}

	[Fact]
	public void WhenToUse_IsIdentical_AcrossProfiles()
	{
		var publicInstructions = McpServerProfile.Public.ComposeServerInstructions();
		var internalInstructions = McpServerProfile.Internal.ComposeServerInstructions();

		var publicBullets = ExtractBullets(publicInstructions);
		var internalBullets = ExtractBullets(internalInstructions);

		foreach (var bullet in internalBullets)
			publicBullets.Should().Contain(bullet);
	}

	[Fact]
	public void Resolve_WithPublic_ReturnsPublicProfile()
	{
		var profile = McpServerProfile.Resolve("public");

		profile.Should().Be(McpServerProfile.Public);
		profile.Name.Should().Be("public");
	}

	[Fact]
	public void Resolve_WithInternal_ReturnsInternalProfile()
	{
		var profile = McpServerProfile.Resolve("internal");

		profile.Should().Be(McpServerProfile.Internal);
		profile.Name.Should().Be("internal");
	}

	[Fact]
	public void Resolve_WithNullOrWhitespace_ReturnsPublicProfile()
	{
		McpServerProfile.Resolve(null).Should().Be(McpServerProfile.Public);
		McpServerProfile.Resolve("").Should().Be(McpServerProfile.Public);
		McpServerProfile.Resolve("   ").Should().Be(McpServerProfile.Public);
	}

	[Fact]
	public void Resolve_WithUnknownProfile_Throws()
	{
		var act = () => McpServerProfile.Resolve("unknown");

		act.Should().Throw<ArgumentException>()
			.WithMessage("*Unknown MCP server profile*")
			.WithParameterName("name");
	}

	[Fact]
	public void PublicProfile_ComposesExactInstructions()
	{
		var instructions = McpServerProfile.Public.ComposeServerInstructions();

		var expected = """
			Use this server to search, retrieve, analyze, and author Elastic product documentation published at elastic.co/docs.

			<triggers>
			Use the server when the user:
			- Wants to find, read, or verify existing documentation pages.
			- Needs to check whether a topic is already documented or how it is covered.
			- References documentation URLs or Elastic product names such as Elasticsearch, Kibana, Fleet, APM, Logstash, Beats, Elastic Security, Elastic Observability, or Elastic Cloud.
			- Asks about documentation structure, coherence, or inconsistencies across pages.
			- Mentions cross-links between documentation repositories (e.g. 'docs-content://path/to/page.md').
			- Is writing or editing documentation and needs to find related content or check consistency.
			- Wants to generate documentation templates following Elastic's content type guidelines.
			</triggers>

			<tool_guidance>
			- Prefer SemanticSearch over a general web search when looking up Elastic documentation content.
			- Use FindRelatedDocs when exploring what documentation exists around a topic.
			- Use GetDocumentByUrl to retrieve a specific page when the user provides or you already know the URL.
			- Use CheckCoherence or FindInconsistencies when reviewing or auditing documentation quality.
			- Use the cross-link tools (ResolveCrossLink, ValidateCrossLinks, FindCrossLinks) when working with links between documentation source repositories.
			- Use ListContentTypes, GetContentTypeGuidelines, and GenerateTemplate when creating new pages.
			</tool_guidance>
			""";

		instructions.Should().Be(expected);
	}

	[Fact]
	public void InternalProfile_ComposesExactInstructions()
	{
		var instructions = McpServerProfile.Internal.ComposeServerInstructions();

		var expected = """
			Use this server to search and retrieve Elastic Internal Docs: team processes, run books, architecture, and other internal knowledge.

			<triggers>
			Use the server when the user:
			- Wants to find, read, or verify existing documentation pages.
			- Needs to check whether a topic is already documented or how it is covered.
			- References documentation URLs or Elastic product names such as Elasticsearch, Kibana, Fleet, APM, Logstash, Beats, Elastic Security, Elastic Observability, or Elastic Cloud.
			</triggers>

			<tool_guidance>
			- Prefer SemanticSearch over a general web search when looking up Elastic documentation content.
			- Use FindRelatedDocs when exploring what documentation exists around a topic.
			- Use GetDocumentByUrl to retrieve a specific page when the user provides or you already know the URL.
			</tool_guidance>
			""";

		instructions.Should().Be(expected);
	}

	private static List<string> ExtractBullets(string instructions) =>
		instructions
			.Split('\n')
			.Where(l => l.TrimStart().StartsWith("- ", StringComparison.Ordinal))
			.Select(l => l.TrimStart()[2..])
			.ToList();
}
