// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote;
using Nullean.Snapshots;

namespace Mcp.Remote.Tests;

public class McpServerInstructionTests
{
	[Test]
	public void PublicProfile_ServiceName_IsDocsMcp() => McpServerProfile.Public.ServiceName.Should().Be("docs-mcp");

	[Test]
	public void InternalProfile_ServiceName_IsCodexMcp() => McpServerProfile.Internal.ServiceName.Should().Be("codex-mcp");

	[Test]
	public void Resolve_WithPublic_ReturnsPublicProfile()
	{
		var profile = McpServerProfile.Resolve("public");

		profile.Should().Be(McpServerProfile.Public);
		profile.Name.Should().Be("public");
	}

	[Test]
	public void Resolve_WithInternal_ReturnsInternalProfile()
	{
		var profile = McpServerProfile.Resolve("internal");

		profile.Should().Be(McpServerProfile.Internal);
		profile.Name.Should().Be("internal");
	}

	[Test]
	public void Resolve_WithNullOrWhitespace_ReturnsPublicProfile()
	{
		McpServerProfile.Resolve(null).Should().Be(McpServerProfile.Public);
		McpServerProfile.Resolve("").Should().Be(McpServerProfile.Public);
		McpServerProfile.Resolve("   ").Should().Be(McpServerProfile.Public);
	}

	[Test]
	public void Resolve_WithUnknownProfile_Throws()
	{
		var act = () => McpServerProfile.Resolve("unknown");

		act.Should().Throw<ArgumentException>().WithMessage("*Unknown MCP server profile*").WithParameterName("name");
	}

	[Test]
	public void PublicProfile_ComposesExactInstructions()
	{
		var instructions = McpServerProfile.Public.ComposeServerInstructions();

		instructions.Should().MatchInlineSnapshot(
			"""
			Use this server to search, retrieve, and analyze Elastic product documentation published at elastic.co/docs. All content covers version 9 and later.

			<triggers>
			Use the server when the user:
			- Wants to find, read, or verify Elastic documentation pages.
			- Needs to check whether a topic is already covered in Elastic documentation.
			- Asks about Elastic documentation structure, coherence, or inconsistencies across pages.
			- References Elastic product names such as Elasticsearch, Kibana, Fleet, APM, Logstash, Beats, Elastic Security, Elastic Observability, or Elastic Cloud.
			</triggers>

			<tool_guidance>
			- Prefer search_docs over a general web search when looking up Elastic documentation content.
			- Use find_related_docs when exploring what documentation exists around a topic.
			- Use get_document_by_url to retrieve a specific page when the user provides or you already know the URL.
			- Use check_docs_coherence or find_docs_inconsistencies when reviewing or auditing documentation quality.
			</tool_guidance>
			"""
		);
	}

	[Test]
	public void InternalProfile_ComposesExactInstructions()
	{
		var instructions = McpServerProfile.Internal.ComposeServerInstructions();

		instructions.Should().MatchInlineSnapshot(
			"""
			Use this server to search and retrieve Elastic internal documentation: team processes, run books, architecture, and other internal knowledge.

			<triggers>
			Use the server when the user:
			- Wants to find, read, or verify Elastic internal documentation pages.
			- Needs to check whether a topic is already covered in Elastic internal documentation.
			- Asks about internal team processes, run books, architecture decisions, or operational knowledge.
			</triggers>

			<tool_guidance>
			- Prefer search_internal_docs over a general web search when looking up Elastic documentation content.
			- Use find_related_internal_docs when exploring what documentation exists around a topic.
			- Use get_internal_document_by_url to retrieve a specific page when the user provides or you already know the URL.
			</tool_guidance>
			"""
		);
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
