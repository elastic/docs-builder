// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Landing;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Schemas;

public class SchemaViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiSchema Schema { get; init; }

	protected override IReadOnlyList<ApiTocItem> GetTocItems()
	{
		var openApiSchema = Schema.Schema;
		var isAggregation = Schema.Category == "aggregations";
		var tocItems = new List<ApiTocItem>();

		// Description
		if (!string.IsNullOrEmpty(openApiSchema.Description))
			tocItems.Add(new ApiTocItem("Description", "description"));

		// Enum values
		if (openApiSchema.Enum is { Count: > 0 })
			tocItems.Add(new ApiTocItem("Enum Values", "enum-values"));

		// Union types (oneOf or anyOf)
		if (openApiSchema.OneOf is { Count: > 0 } || openApiSchema.AnyOf is { Count: > 0 })
			tocItems.Add(new ApiTocItem("Union Types", "union-types"));

		// Aggregation request (for aggregations)
		if (isAggregation && Schema.RelatedAggregation is not null)
		{
			tocItems.Add(new ApiTocItem("Aggregation Request", "aggregation-request"));
			// Add top-level properties nested under Aggregation Request
			var aggProps = GetSchemaPropertyNames(Schema.RelatedAggregation);
			foreach (var propName in aggProps)
				tocItems.Add(new ApiTocItem(propName, $"agg-{propName}", 3));
		}
		// Properties (for non-aggregations with properties)
		else if (HasSchemaProperties(openApiSchema))
		{
			tocItems.Add(new ApiTocItem("Properties", "properties"));
			// Add top-level properties nested under Properties
			var props = GetSchemaPropertyNames(openApiSchema);
			foreach (var propName in props)
				tocItems.Add(new ApiTocItem(propName, propName, 3));
		}

		// Aggregate response (for aggregations)
		if (isAggregation && Schema.RelatedAggregate is not null)
		{
			tocItems.Add(new ApiTocItem("Aggregate Response", "aggregate-response"));
			// Add top-level properties nested under Aggregate Response
			var resultProps = GetSchemaPropertyNames(Schema.RelatedAggregate);
			foreach (var propName in resultProps)
				tocItems.Add(new ApiTocItem(propName, $"result-{propName}", 3));
		}

		// Additional properties
		if (openApiSchema.AdditionalProperties is not null)
			tocItems.Add(new ApiTocItem("Additional Properties", "additional-properties"));

		// Example
		if (openApiSchema.Example is not null)
			tocItems.Add(new ApiTocItem("Example", "example"));

		return tocItems;
	}

	/// <summary>
	/// Gets the property names from a schema, resolving references and AllOf as needed.
	/// </summary>
	private IEnumerable<string> GetSchemaPropertyNames(IOpenApiSchema? schema)
	{
		if (schema is null)
			return [];

		// Handle schema references
		if (schema is OpenApiSchemaReference schemaRef)
		{
			if (schemaRef.Properties is { Count: > 0 })
				return schemaRef.Properties.Keys;

			var refId = schemaRef.Reference?.Id;
			if (!string.IsNullOrEmpty(refId) &&
				Document.Components?.Schemas?.TryGetValue(refId, out var resolvedSchema) == true)
			{
				return GetSchemaPropertyNames(resolvedSchema);
			}
		}

		// Direct properties
		if (schema.Properties is { Count: > 0 })
			return schema.Properties.Keys;

		// For allOf, collect property names from all schemas
		if (schema.AllOf is { Count: > 0 })
		{
			var allProps = new List<string>();
			foreach (var subSchema in schema.AllOf)
				allProps.AddRange(GetSchemaPropertyNames(subSchema));
			return allProps.Distinct();
		}

		return [];
	}

	/// <summary>
	/// Checks if a schema has properties, resolving references and AllOf as needed.
	/// </summary>
	private bool HasSchemaProperties(IOpenApiSchema? schema)
	{
		if (schema is null)
			return false;

		// Handle schema references - resolve to get actual properties
		if (schema is OpenApiSchemaReference schemaRef)
		{
			// Try direct property access first (proxied)
			if (schemaRef.Properties is { Count: > 0 })
				return true;

			// Try resolving via Reference.Id
			var refId = schemaRef.Reference?.Id;
			if (!string.IsNullOrEmpty(refId) &&
				Document.Components?.Schemas?.TryGetValue(refId, out var resolvedSchema) == true)
			{
				return HasSchemaProperties(resolvedSchema);
			}
		}

		// Direct properties
		if (schema.Properties is { Count: > 0 })
			return true;

		// For allOf, check if any sub-schema has properties
		if (schema.AllOf is { Count: > 0 })
		{
			foreach (var subSchema in schema.AllOf)
			{
				if (HasSchemaProperties(subSchema))
					return true;
			}
		}

		return false;
	}
}
