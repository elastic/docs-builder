// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Types;

public class SchemaViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiSchema Schema { get; init; }

	/// <summary>Precomputed structural content of the page; built before the slice renders.</summary>
	public required SchemaPageModel Page { get; init; }

	protected override string? LayoutPageTitle => Schema.DisplayName;

	protected override string? LayoutPageDescription => Schema.Schema.Description;

	protected override IReadOnlyList<ApiTocItem> GetTocItems()
	{
		var openApiSchema = Schema.Schema;
		var tocItems = new List<ApiTocItem>();

		// Description
		if (!string.IsNullOrEmpty(openApiSchema.Description))
			tocItems.Add(new ApiTocItem("Description", "description"));

		// Enum values
		if (Page.EnumValues.Count > 0)
			tocItems.Add(new ApiTocItem("Enum Values", "enum-values"));

		// Union types (oneOf or anyOf)
		if (Page.UnionVariants is not null)
			tocItems.Add(new ApiTocItem("Union Types", "union-types"));

		// Properties
		if (Page.Properties is { Items.Count: > 0 } properties)
		{
			tocItems.Add(new ApiTocItem("Properties", "properties"));
			foreach (var property in properties.Items)
				tocItems.Add(new ApiTocItem(property.Name, property.Name, 3));
		}

		// Additional properties
		if (openApiSchema.AdditionalProperties is not null)
			tocItems.Add(new ApiTocItem("Additional Properties", "additional-properties"));

		// Example
		if (openApiSchema.Examples is { Count: > 0 })
			tocItems.Add(new ApiTocItem("Example", "example"));

		return tocItems;
	}
}
