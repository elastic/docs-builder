// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Types;

internal static class SchemaCommonMark
{
	public static string Write(ApiSchema schema, SchemaPageModel page, ApiRenderContext context)
	{
		var markdown = new StringBuilder();
		var apiBaseUrl = context.CurrentNavigation.NavigationRoot.Url;
		var openApiSchema = schema.Schema;
		ApiCommonMark.Heading(markdown, 1, schema.DisplayName);
		ApiCommonMark.Paragraph(markdown, $"`{schema.SchemaId}`");

		if (!string.IsNullOrEmpty(page.DictionaryTypeName))
		{
			ApiCommonMark.Paragraph(markdown, $"`{page.DictionaryTypeName}`");
			ApiCommonMark.Paragraph(markdown, $"This type represents a dictionary mapping string keys to `{schema.DisplayName}` values.");
		}

		if (!string.IsNullOrEmpty(openApiSchema.Description))
		{
			ApiCommonMark.Heading(markdown, 2, "Description");
			ApiCommonMark.Prepared(markdown, openApiSchema.Description, apiBaseUrl);
		}

		if (page.ExternalDocs is { } docs)
			ApiCommonMark.Paragraph(markdown, ApiCommonMark.Link(docs.LinkText, docs.Url));

		if (openApiSchema.Enum is { Count: > 0 })
		{
			ApiCommonMark.Heading(markdown, 2, "Enum Values");
			foreach (var enumValue in openApiSchema.Enum)
				_ = markdown.AppendLine($"- `{enumValue}`");
			_ = markdown.AppendLine();
		}

		WriteUnion(markdown, page.OneOfVariants, "Union Types (oneOf)", "This type can be one of the following:", apiBaseUrl);
		WriteUnion(markdown, page.AnyOfVariants, "Union Types (anyOf)", "This type can be any of the following:", apiBaseUrl);

		if (page.Properties is not null)
		{
			ApiCommonMark.Heading(markdown, 2, "Properties");
			ApiPropertyMarkdown.WriteList(markdown, page.Properties, apiBaseUrl);
			_ = markdown.AppendLine();
		}

		if (page.AdditionalPropertiesType is not null)
		{
			ApiCommonMark.Heading(markdown, 2, "Additional Properties");
			ApiPropertyMarkdown.WriteType(markdown, page.AdditionalPropertiesType);
		}

		if (openApiSchema.Example is not null)
		{
			ApiCommonMark.Heading(markdown, 2, "Example");
			ApiCommonMark.Fence(markdown, "json", openApiSchema.Example.ToString());
		}

		return markdown.ToString();
	}

	private static void WriteUnion(StringBuilder markdown, ApiUnionVariants? variants, string heading, string intro, string apiBaseUrl)
	{
		if (variants is null)
			return;

		ApiCommonMark.Heading(markdown, 2, heading);
		ApiCommonMark.Paragraph(markdown, intro);
		ApiPropertyMarkdown.WriteVariants(markdown, variants, apiBaseUrl);
		_ = markdown.AppendLine();
	}
}
