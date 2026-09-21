// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Supplemental;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Infrastructure;

public sealed record ApiVersionSwitcherItem(string Label, string Url, bool Selected);

public record ApiRenderContext(
	BuildContext BuildContext,
	OpenApiDocument Model,
	StaticFileContentHashProvider StaticFileContentHashProvider
) : RenderContext<OpenApiDocument>(BuildContext, Model)
{
	public required string NavigationHtml { get; init; }
	public required INavigationItem CurrentNavigation { get; init; }
	public required IMarkdownStringRenderer MarkdownRenderer { get; init; }

	/// <summary>Logger for API Explorer rendering (e.g. OpenAPI extension parsing); optional when the host does not provide one.</summary>
	public ILogger? ApiExplorerLog { get; init; }

	public IReadOnlyList<ApiVersionSwitcherItem> VersionSwitcherItems { get; init; } = [];

	public IReadOnlyList<ApiCatalogEntry> CatalogEntries { get; init; } = [];

	public string? CurrentApiKey { get; init; }

	/// <summary>Product bound to this API key, or <see langword="null"/> on the combined catalog.</summary>
	public Product? Product { get; init; }

	internal IReadOnlyDictionary<string, ApiSupplementalDoc> OperationSupplemental
	{
		get;
		init;
	} = FrozenDictionary<string, ApiSupplementalDoc>.Empty;

	internal IReadOnlyDictionary<string, ApiSupplementalDoc> TagSupplemental
	{
		get;
		init;
	} = FrozenDictionary<string, ApiSupplementalDoc>.Empty;
}
