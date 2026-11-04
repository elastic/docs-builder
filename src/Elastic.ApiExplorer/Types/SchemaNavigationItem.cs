// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Microsoft.OpenApi;
using RazorSlices;

namespace Elastic.ApiExplorer.Types;

public record ApiSchema(string SchemaId, string DisplayName, string Category, IOpenApiSchema Schema) : IApiModel<SchemaPageModel>
{
	public SchemaPageModel? CreatePageModel(ApiRenderContext context) => SchemaPageModel.Create(this, context);

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, SchemaPageModel? pageModel, Cancel ctx = default)
	{
		var page = pageModel ?? SchemaPageModel.Create(this, context);
		var viewModel = new SchemaViewModel(context) { Schema = this, Page = page };
		var slice = SchemaView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}

	public Task<string?> RenderCommonMarkAsync(ApiRenderContext context, SchemaPageModel? pageModel, Cancel ctx = default)
	{
		var page = pageModel ?? SchemaPageModel.Create(this, context);
		return Task.FromResult<string?>(SchemaCommonMark.Write(this, page, context));
	}
}

public class SchemaNavigationItem(
	string? urlPathPrefix,
	string apiUrlSuffix,
	ApiSchema apiSchema,
	IRootNavigationItem<IApiGroupingModel, INavigationItem> root,
	IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parent
) : ApiPageNavigationItem<ApiSchema>(
	$"{ApiUrlBuilder.ProductRoot(urlPathPrefix, apiUrlSuffix)}/types/{ApiUrlBuilder.SchemaMoniker(apiSchema.SchemaId)}",
	apiSchema.DisplayName,
	apiSchema,
	root,
	parent
);

public record SchemaCategory(string Name, string Description) : IApiGroupingModel
{
	public Task RenderAsync(FileSystemStream stream, ApiRenderContext context, CancellationToken ctx = default) => Task.CompletedTask;
}

/// <summary>The Types sidebar heading. It groups the shared type pages after the endpoint groups.</summary>
public class TypesNavigationItem(LandingNavigationItem rootNavigation) : ApiSidebarHeadingNavigationItem(Title, rootNavigation)
{
	public const string Title = "Types";
}

public class SchemaCategoryNavigationItem(
	SchemaCategory category,
	IRootNavigationItem<IApiGroupingModel, INavigationItem> root,
	IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parent
) : ApiGroupingNavigationItem<SchemaCategory, INavigationItem>(category, root, parent)
{
	public override string NavigationTitle { get; } = category.Name;
	public override string Id { get; } = ShortId.Create("schema-category", category.Name);
}
