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
/// <param name="WhenToUse">Generic bullet points for the "Use the server when the user:" section.</param>
/// <param name="ToolGuidance">Lines for the tool guidance section.</param>
/// <param name="RegisterServices">DI registrations the module's tools depend on.</param>
public sealed record McpFeatureModule(
	string Name,
	string? Capability,
	string[] WhenToUse,
	string[] ToolGuidance,
	Action<IServiceCollection> RegisterServices
);

internal static class McpFeatureModules
{
	public static readonly McpFeatureModule Search = new(
		Name: "Search",
		Capability: "search",
		WhenToUse:
		[
			"Wants to find, read, or verify existing documentation pages.",
			"Needs to check whether a topic is already documented or how it is covered.",
			"References documentation URLs or Elastic product names such as Elasticsearch, Kibana, Fleet, APM, Logstash, Beats, Elastic Security, Elastic Observability, or Elastic Cloud."
		],
		ToolGuidance:
		[
			"Prefer SemanticSearch over a general web search when looking up Elastic documentation content.",
			"Use FindRelatedDocs when exploring what documentation exists around a topic."
		],
		RegisterServices: services => _ = services.AddSearchServices()
	);

	public static readonly McpFeatureModule Documents = new(
		Name: "Documents",
		Capability: "retrieve",
		WhenToUse: [],
		ToolGuidance:
		[
			"Use GetDocumentByUrl to retrieve a specific page when the user provides or you already know the URL."
		],
		RegisterServices: services => _ = services.AddScoped<IDocumentGateway, DocumentGateway>()
	);

	public static readonly McpFeatureModule Coherence = new(
		Name: "Coherence",
		Capability: "analyze",
		WhenToUse:
		[
			"Asks about documentation structure, coherence, or inconsistencies across pages."
		],
		ToolGuidance:
		[
			"Use CheckCoherence or FindInconsistencies when reviewing or auditing documentation quality."
		],
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
			"Use the cross-link tools (ResolveCrossLink, ValidateCrossLinks, FindCrossLinks) when working with links between documentation source repositories."
		],
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
			"Is writing or editing documentation and needs to find related content or check consistency.",
			"Wants to generate documentation templates following Elastic's content type guidelines."
		],
		ToolGuidance:
		[
			"Use ListContentTypes, GetContentTypeGuidelines, and GenerateTemplate when creating new pages."
		],
		RegisterServices: services => _ = services.AddSingleton<ContentTypeProvider>()
	);
}
