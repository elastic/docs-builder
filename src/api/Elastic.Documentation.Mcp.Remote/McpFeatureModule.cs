// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Assembler.Mcp;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links.InboundLinks;
using Elastic.Documentation.Mcp.Remote.Gateways;
using Elastic.Documentation.Mcp.Remote.Tools;
using Elastic.Documentation.Search;
using Microsoft.Extensions.DependencyInjection;

namespace Elastic.Documentation.Mcp.Remote;

/// <summary>
/// A feature module with DI services and instruction template fragments.
/// WhenToUse bullets should be generic and context-free; the branding introduction frames their meaning.
/// </summary>
/// <param name="Name">Module identifier.</param>
/// <param name="Capability">Capability verb for the preamble (e.g. "search", "retrieve"). Null if the module does not add a capability.</param>
/// <param name="WhenToUse">Bullet points for the "Use the server when the user:" section. Use {docs} for the profile's DocsDescription.</param>
/// <param name="ToolGuidance">Lines for the tool guidance section. Use {tool:snake_case_name} for tool names (e.g. {tool:semantic_search}).</param>
/// <param name="ToolType">The tool class type (e.g. typeof(SearchTools)). Null if the module has no tools.</param>
/// <param name="RegisterServices">DI registrations the module's tools depend on.</param>
public sealed record McpFeatureModule(
	string Name,
	string? Capability,
	string[] WhenToUse,
	string[] ToolGuidance,
	Type? ToolType,
	Action<IServiceCollection> RegisterServices
);

internal static class McpFeatureModules
{
	public static readonly McpFeatureModule Search = new(
		Name: "Search",
		Capability: "search",
		WhenToUse:
		[
			"Wants to find, read, or verify {docs} pages.",
			"Needs to check whether a topic is already covered in {docs}."
		],
		ToolGuidance:
		[
			"Prefer {tool:semantic_search} over a general web search when looking up Elastic documentation content.",
			"Use {tool:find_related_docs} when exploring what documentation exists around a topic."
		],
		ToolType: typeof(SearchTools),
		RegisterServices: services => _ = services.AddSearchServices()
	);

	public static readonly McpFeatureModule Documents = new(
		Name: "Documents",
		Capability: "retrieve",
		WhenToUse: [],
		ToolGuidance:
		[
			"Use {tool:get_document_by_url} to retrieve a specific page when the user provides or you already know the URL."
		],
		ToolType: typeof(DocumentTools),
		RegisterServices: services => _ = services.AddScoped<IDocumentGateway, DocumentGateway>()
	);

	public static readonly McpFeatureModule Coherence = new(
		Name: "Coherence",
		Capability: "analyze",
		WhenToUse:
		[
			"Asks about {docs} structure, coherence, or inconsistencies across pages."
		],
		ToolGuidance:
		[
			"Use {tool:check_coherence} or {tool:find_inconsistencies} when reviewing or auditing documentation quality."
		],
		ToolType: typeof(CoherenceTools),
		RegisterServices: _ => { }
	);

	public static readonly McpFeatureModule Links = new(
		Name: "Links",
		Capability: null,
		WhenToUse:
		[
			"Mentions cross-links between documentation repositories (e.g. 'docs-content://path/to/page.md')."
		],
		ToolGuidance:
		[
			"Use the cross-link tools ({tool:resolve_cross_link}, {tool:validate_cross_links}, {tool:find_cross_links}) when working with links between documentation source repositories."
		],
		ToolType: typeof(LinkTools),
		RegisterServices: services =>
		{
			_ = services.AddSingleton<ILinkIndexReader>(_ => Aws3LinkIndexReader.CreateAnonymous());
			_ = services.AddSingleton<LinksIndexCrossLinkFetcher>();
			_ = services.AddSingleton<ILinkUtilService, LinkUtilService>();
		}
	);

	public static readonly McpFeatureModule ContentTypes = new(
		Name: "ContentTypes",
		Capability: "author",
		WhenToUse:
		[
			"Is writing or editing {docs} and needs to find related content or check consistency.",
			"Wants to generate {docs} templates following Elastic's content type guidelines."
		],
		ToolGuidance:
		[
			"Use {tool:list_content_types}, {tool:get_content_type_guidelines}, and {tool:generate_template} when creating new pages."
		],
		ToolType: typeof(ContentTypeTools),
		RegisterServices: services => _ = services.AddSingleton<ContentTypeProvider>()
	);
}
