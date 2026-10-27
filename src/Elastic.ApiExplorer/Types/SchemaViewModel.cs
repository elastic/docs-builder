// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Types;

public class SchemaViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiSchema Schema { get; init; }

	/// <summary>Precomputed structural content of the page; built before the slice renders.</summary>
	public required SchemaPageModel Page { get; init; }

	protected override string? LayoutPageTitle => Schema.DisplayName;

	protected override string? LayoutPageDescription => Schema.Schema.Description;

	protected override IReadOnlyList<TocItem> GetTocItems()
	{
		var openApiSchema = Schema.Schema;
		var tocItems = new List<TocItem>();

		// Description
		if (!string.IsNullOrEmpty(openApiSchema.Description))
			tocItems.Add(new TocItem("Description", "description", 2));

		// Enum values
		if (Page.EnumValues.Count > 0)
			tocItems.Add(new TocItem("Enum values", "enum-values", 2));

		// Union types (oneOf or anyOf)
		if (Page.UnionVariants is not null)
			tocItems.Add(new TocItem("Union types", "union-types", 2));

		// Properties
		if (Page.Properties is { Items.Count: > 0 } properties)
		{
			tocItems.Add(new TocItem("Properties", "properties", 2));
			foreach (var property in properties.Items)
				tocItems.Add(new TocItem(property.Name, property.Name, 3));
		}

		// Additional properties
		if (openApiSchema.AdditionalProperties is not null)
			tocItems.Add(new TocItem("Additional properties", "additional-properties", 2));

		// Example
		if (openApiSchema.Examples is { Count: > 0 })
			tocItems.Add(new TocItem("Example", "example", 2));

		return tocItems;
	}
}
