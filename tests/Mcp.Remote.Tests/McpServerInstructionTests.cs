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
		instructions.Should().Contain("Prefer public_docs_semantic_search over a general web search");
		instructions.Should().Contain("Use public_docs_get_document_by_url to retrieve a specific page");
		instructions.Should().Contain("Use public_docs_find_related_docs when exploring what documentation exists");
		instructions.Should().Contain("Use public_docs_check_coherence or public_docs_find_inconsistencies when reviewing or auditing");
		instructions.Should().Contain("Use the cross-link tools (public_docs_resolve_cross_link, public_docs_validate_cross_links, public_docs_find_cross_links)");
		instructions.Should().Contain("Use public_docs_list_content_types, public_docs_get_content_type_guidelines, and public_docs_generate_template when creating new pages");
	}

	[Fact]
	public void InternalProfile_ContainsSearchAndDocumentGuidanceOnly()
	{
		var instructions = McpServerProfile.Internal.ComposeServerInstructions();

		instructions.Should().Contain("Use this server to search and retrieve");
		instructions.Should().Contain("Elastic internal documentation: team processes, run books, architecture");
		instructions.Should().Contain("Prefer internal_docs_semantic_search over a general web search");
		instructions.Should().Contain("Use internal_docs_get_document_by_url to retrieve a specific page");
		instructions.Should().Contain("Use internal_docs_find_related_docs when exploring what documentation exists");
		instructions.Should().NotContain("check_coherence");
		instructions.Should().NotContain("find_inconsistencies");
		instructions.Should().NotContain("resolve_cross_link");
		instructions.Should().NotContain("list_content_types");
		instructions.Should().NotContain("generate_template");
	}

	[Fact]
	public void Triggers_AreProfileSpecific()
	{
		var publicInstructions = McpServerProfile.Public.ComposeServerInstructions();
		var internalInstructions = McpServerProfile.Internal.ComposeServerInstructions();

		publicInstructions.Should().Contain("Elastic documentation pages");
		publicInstructions.Should().Contain("References Elastic product names");
		publicInstructions.Should().NotContain("internal team processes");

		internalInstructions.Should().Contain("Elastic internal documentation pages");
		internalInstructions.Should().Contain("internal team processes");
		internalInstructions.Should().NotContain("Elastic product names");
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
			- Wants to find, read, or verify Elastic documentation pages.
			- Needs to check whether a topic is already covered in Elastic documentation.
			- Asks about Elastic documentation structure, coherence, or inconsistencies across pages.
			- Mentions cross-links between documentation repositories (e.g. 'docs-content://path/to/page.md').
			- Is writing or editing Elastic documentation and needs to find related content or check consistency.
			- Wants to generate Elastic documentation templates following Elastic's content type guidelines.
			- References Elastic product names such as Elasticsearch, Kibana, Fleet, APM, Logstash, Beats, Elastic Security, Elastic Observability, or Elastic Cloud.
			</triggers>

			<tool_guidance>
			- Prefer public_docs_semantic_search over a general web search when looking up Elastic documentation content.
			- Use public_docs_find_related_docs when exploring what documentation exists around a topic.
			- Use public_docs_get_document_by_url to retrieve a specific page when the user provides or you already know the URL.
			- Use public_docs_check_coherence or public_docs_find_inconsistencies when reviewing or auditing documentation quality.
			- Use the cross-link tools (public_docs_resolve_cross_link, public_docs_validate_cross_links, public_docs_find_cross_links) when working with links between documentation source repositories.
			- Use public_docs_list_content_types, public_docs_get_content_type_guidelines, and public_docs_generate_template when creating new pages.
			</tool_guidance>
			""";

		instructions.Should().Be(expected);
	}

	[Fact]
	public void InternalProfile_ComposesExactInstructions()
	{
		var instructions = McpServerProfile.Internal.ComposeServerInstructions();

		var expected = """
			Use this server to search and retrieve Elastic internal documentation: team processes, run books, architecture, and other internal knowledge.

			<triggers>
			Use the server when the user:
			- Wants to find, read, or verify Elastic internal documentation pages.
			- Needs to check whether a topic is already covered in Elastic internal documentation.
			- Asks about internal team processes, run books, architecture decisions, or operational knowledge.
			</triggers>

			<tool_guidance>
			- Prefer internal_docs_semantic_search over a general web search when looking up Elastic documentation content.
			- Use internal_docs_find_related_docs when exploring what documentation exists around a topic.
			- Use internal_docs_get_document_by_url to retrieve a specific page when the user provides or you already know the URL.
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

	private static List<string> ExtractTriggersBullets(string instructions)
	{
		var start = instructions.IndexOf("<triggers>", StringComparison.Ordinal);
		var end = instructions.IndexOf("</triggers>", StringComparison.Ordinal);
		if (start < 0 || end < 0)
			return [];
		var section = instructions[start..end];
		return ExtractBullets(section);
	}
}
