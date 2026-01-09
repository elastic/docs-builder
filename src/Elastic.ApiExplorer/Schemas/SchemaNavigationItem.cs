// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Microsoft.OpenApi;
using RazorSlices;

namespace Elastic.ApiExplorer.Schemas;

public record ApiSchema(string SchemaId, string DisplayName, string Category, IOpenApiSchema Schema) : IApiModel
{
	// For aggregations, we may have both an Aggregation and Aggregate type
	public IOpenApiSchema? RelatedAggregate { get; init; }
	public IOpenApiSchema? RelatedAggregation { get; init; }

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var viewModel = new SchemaViewModel(context)
		{
			Schema = this
		};
		var slice = SchemaView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}
}

public class SchemaNavigationItem : ILeafNavigationItem<ApiSchema>
{
	public SchemaNavigationItem(
		string? urlPathPrefix,
		string apiUrlSuffix,
		ApiSchema apiSchema,
		IRootNavigationItem<IApiGroupingModel, INavigationItem> root,
		IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parent
	)
	{
		NavigationRoot = root;
		Model = apiSchema;
		NavigationTitle = apiSchema.DisplayName;
		Parent = parent;
		var moniker = apiSchema.SchemaId.Replace('.', '-').ToLowerInvariant();
		Url = $"{urlPathPrefix?.TrimEnd('/')}/api/{apiUrlSuffix}/types/{moniker}";
		Id = ShortId.Create(Url);
	}

	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
	public string Id { get; }
	public ApiSchema Model { get; }
	public string Url { get; }
	public bool Hidden { get; set; }
	public string NavigationTitle { get; }
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public int NavigationIndex { get; set; }
}

public record SchemaCategory(string Name, string Description) : IApiGroupingModel
{
	public Task RenderAsync(FileSystemStream stream, ApiRenderContext context, CancellationToken ctx = default) => Task.CompletedTask;
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
