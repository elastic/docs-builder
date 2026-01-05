// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Schema;

/// <summary>
/// Renders validation constraints for OpenAPI schema properties.
/// </summary>
public static class ValidationConstraintsRenderer
{
	/// <summary>
	/// Renders validation constraints for a schema as HTML.
	/// </summary>
	public static IHtmlContent Render(IOpenApiSchema schema)
	{
		var constraints = new List<string>();

		// Default value
		if (schema.Default != null)
		{
			var defaultStr = schema.Default.ToString();
			if (!string.IsNullOrEmpty(defaultStr))
				constraints.Add($"default: <code>{System.Web.HttpUtility.HtmlEncode(defaultStr)}</code>");
		}

		// String constraints
		if (schema.MinLength.HasValue)
			constraints.Add($"min length: {schema.MinLength.Value}");
		if (schema.MaxLength.HasValue)
			constraints.Add($"max length: {schema.MaxLength.Value}");
		if (!string.IsNullOrEmpty(schema.Pattern))
			constraints.Add($"pattern: <code>{System.Web.HttpUtility.HtmlEncode(schema.Pattern)}</code>");

		// Numeric constraints (these are strings in OpenAPI library)
		if (!string.IsNullOrEmpty(schema.Minimum))
			constraints.Add($"min: {schema.Minimum}");
		if (!string.IsNullOrEmpty(schema.Maximum))
			constraints.Add($"max: {schema.Maximum}");
		if (!string.IsNullOrEmpty(schema.ExclusiveMinimum))
			constraints.Add($"exclusive min: {schema.ExclusiveMinimum}");
		if (!string.IsNullOrEmpty(schema.ExclusiveMaximum))
			constraints.Add($"exclusive max: {schema.ExclusiveMaximum}");
		if (schema.MultipleOf.HasValue)
			constraints.Add($"multiple of: {schema.MultipleOf.Value}");

		// Array constraints
		if (schema.MinItems.HasValue)
			constraints.Add($"min items: {schema.MinItems.Value}");
		if (schema.MaxItems.HasValue)
			constraints.Add($"max items: {schema.MaxItems.Value}");
		if (schema.UniqueItems == true)
			constraints.Add("unique items");

		if (constraints.Count == 0)
			return HtmlString.Empty;

		var sb = new System.Text.StringBuilder();
		_ = sb.Append("<dd class=\"validation-constraints\"><span class=\"constraints-label\">Constraints:</span>");
		foreach (var constraint in constraints)
		{
			_ = sb.Append($"<span class=\"constraint\">{constraint}</span>");
		}
		_ = sb.Append("</dd>");

		return new HtmlString(sb.ToString());
	}
}
