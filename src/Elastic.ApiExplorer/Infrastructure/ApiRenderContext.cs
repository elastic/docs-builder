// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.V2;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Infrastructure;

public record ApiRenderContext(
	BuildContext BuildContext,
	OpenApiDocument Model,
	StaticFileContentHashProvider StaticFileContentHashProvider
)
	: RenderContext<OpenApiDocument>(BuildContext, Model)
{
	public required string NavigationHtml { get; init; }
	public required INavigationItem CurrentNavigation { get; init; }
	public required IMarkdownStringRenderer MarkdownRenderer { get; init; }

	/// <summary>Logger for API Explorer rendering (e.g. OpenAPI extension parsing); optional when the host does not provide one.</summary>
	public ILogger? ApiExplorerLog { get; init; }

	/// <summary>Assembler Nav V2 section tabs; set when rendering with <see cref="OpenApiNavigationHost"/>.</summary>
	public IReadOnlyList<NavigationSection>? NavV2Sections { get; init; }

	/// <summary>Active Nav V2 section id for the secondary nav bar.</summary>
	public string? ActiveSectionId { get; init; }

	/// <summary>Feature flags from the host build (e.g. assembler <c>NAV_V2</c>).</summary>
	public FeatureFlags? FeatureFlags { get; init; }
}
