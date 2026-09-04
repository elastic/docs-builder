// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

internal static class OperationCommonMark
{
	public static string Write(
		ApiOperation apiOperation,
		OperationPageModel page,
		IReadOnlyList<string>? prerequisites,
		ApiRenderContext context
	)
	{
		var markdown = new StringBuilder();
		var apiBaseUrl = context.CurrentNavigation.NavigationRoot.Url;
		var operation = apiOperation.Operation;
		var title = operation.Summary ?? apiOperation.ApiName;
		ApiCommonMark.Heading(markdown, 1, title);
		WriteBadges(markdown, operation, page);
		WriteServers(markdown, page);
		WritePaths(markdown, apiOperation, page);
		WritePrerequisites(markdown, prerequisites, apiBaseUrl);
		WritePathParameters(markdown, page, apiBaseUrl);
		WriteDescription(markdown, page, apiBaseUrl);
		WriteSecurity(markdown, page);
		WriteQueryParameters(markdown, page, apiBaseUrl);
		WriteRequestBody(markdown, apiOperation, page, apiBaseUrl);
		WriteResponses(markdown, page, apiBaseUrl);
		WriteCodeSamples(markdown, page);
		WriteExamples(markdown, "Request Examples", page.ShowRequestExamples, page.RequestExamples, apiBaseUrl);
		WriteExamples(markdown, "Response Examples", page.ShowResponseExamples, page.ResponseExamples, apiBaseUrl);
		foreach (var extra in page.PostSections)
		{
			ApiCommonMark.Heading(markdown, 2, extra.Heading);
			ApiCommonMark.Prepared(markdown, extra.BodyMarkdown, apiBaseUrl);
		}

		return markdown.ToString();
	}

	private static void WriteBadges(StringBuilder markdown, OpenApiOperation operation, OperationPageModel page)
	{
		if (operation.Deprecated)
			ApiCommonMark.Paragraph(markdown, "deprecated");
		if (page.IsBeta)
			ApiCommonMark.Paragraph(markdown, "Beta");
		if (page.Availability is { } availability)
		{
			var text = availability.ShowVersion
				? $"{availability.BadgeLifecycleText} {availability.BadgeVersion}".Trim()
				: availability.BadgeLifecycleText;
			ApiCommonMark.Paragraph(markdown, $"Availability: {text}");
		}
	}

	private static void WriteServers(StringBuilder markdown, OperationPageModel page)
	{
		if (page.Servers is not { Count: > 0 })
			return;

		foreach (var server in page.Servers)
		{
			var line = $"`{server.Url}`";
			if (!string.IsNullOrEmpty(server.Description))
				line += $" ({server.Description})";
			_ = markdown.AppendLine($"- Server: {line}");
		}

		_ = markdown.AppendLine();
	}

	private static void WritePaths(StringBuilder markdown, ApiOperation current, OperationPageModel page)
	{
		ApiCommonMark.Heading(markdown, 2, "Paths");
		foreach (var overload in page.Overloads)
		{
			var method = overload.Model.OperationType.ToString().ToUpperInvariant();
			var marker = overload.Model.Route == current.Route && overload.Model.OperationType == current.OperationType ? " (current)" : "";
			var deprecated = overload.Model.Operation?.Deprecated == true ? " — deprecated" : "";
			_ = markdown.AppendLine($"- `{method}` `{overload.Model.Route}`{marker}{deprecated}");
		}

		_ = markdown.AppendLine();
	}

	private static void WritePrerequisites(StringBuilder markdown, IReadOnlyList<string>? prerequisites, string apiBaseUrl)
	{
		if (prerequisites is not { Count: > 0 })
			return;

		ApiCommonMark.Heading(markdown, 2, "Prerequisites");
		foreach (var line in prerequisites)
			_ = markdown.AppendLine($"- {ApiMarkdown.Prepare(line, apiBaseUrl)}");
		_ = markdown.AppendLine();
	}

	private static void WritePathParameters(StringBuilder markdown, OperationPageModel page, string apiBaseUrl)
	{
		if (page.PathParameters.Count == 0)
			return;

		ApiCommonMark.Heading(markdown, 4, "Path Parameters");
		foreach (var path in page.PathParameters)
		{
			var deprecated = path.Deprecated is true ? " — deprecated" : "";
			_ = markdown.AppendLine($"- `{path.Name}`{deprecated}");
			var description = ApiMarkdown.Prepare(path.DescriptionMarkdown, apiBaseUrl);
			if (!string.IsNullOrWhiteSpace(description))
				_ = markdown.AppendLine($"  {description.TrimEnd()}");
		}

		_ = markdown.AppendLine();
	}

	private static void WriteDescription(StringBuilder markdown, OperationPageModel page, string apiBaseUrl)
	{
		if (!string.IsNullOrWhiteSpace(page.DescriptionMarkdown))
		{
			ApiCommonMark.Heading(markdown, 2, "Description");
			ApiCommonMark.Prepared(markdown, page.DescriptionMarkdown, apiBaseUrl);
		}

		if (page.ExternalDocs is { } docs)
			ApiCommonMark.Paragraph(markdown, ApiCommonMark.Link(docs.LinkText, docs.Url));
	}

	private static void WriteSecurity(StringBuilder markdown, OperationPageModel page)
	{
		if (page.AuthSchemes.Count == 0)
			return;

		ApiCommonMark.Paragraph(markdown, "Authorization: " + string.Join(", ", page.AuthSchemes.Select(scheme => scheme.Label)));
	}

	private static void WriteQueryParameters(StringBuilder markdown, OperationPageModel page, string apiBaseUrl)
	{
		if (page.QueryParameters.Count == 0)
			return;

		ApiCommonMark.Heading(markdown, 2, "Query String Parameters");
		foreach (var query in page.QueryParameters)
		{
			var parameter = query.Parameter;
			var type = query.Type is null ? "" : $" ({query.Type.Text})";
			var deprecated = parameter.Deprecated ? " — deprecated" : "";
			_ = markdown.AppendLine($"- `{parameter.Name}`{type}{deprecated}");
			var description = ApiMarkdown.Prepare(query.DescriptionMarkdown, apiBaseUrl);
			if (!string.IsNullOrWhiteSpace(description))
				_ = markdown.AppendLine($"  {description.TrimEnd()}");

			if (query.UnionOptions.Count > 0 && query.EnumValues.Count == 0)
				_ = markdown.AppendLine("  One of: " + string.Join(" or ", query.UnionOptions.Select(o => $"`{o.Text}`")));
			if (query.EnumValues.Count > 0)
				_ = markdown.AppendLine("  Values: " + string.Join(", ", query.EnumValues.Select(v => $"`{v}`")));
		}

		_ = markdown.AppendLine();
	}

	private static void WriteRequestBody(StringBuilder markdown, ApiOperation apiOperation, OperationPageModel page, string apiBaseUrl)
	{
		if (apiOperation.Operation.RequestBody is null)
			return;

		ApiCommonMark.Heading(markdown, 2, "Request Body");
		if (!string.IsNullOrEmpty(page.RequestContentType))
			ApiCommonMark.Paragraph(markdown, $"`{page.RequestContentType}`");
		ApiCommonMark.Prepared(markdown, apiOperation.Operation.RequestBody.Description, apiBaseUrl);
		if (page.RequestProperties is not null)
			ApiPropertyMarkdown.WriteList(markdown, page.RequestProperties, apiBaseUrl);
		else
			ApiPropertyMarkdown.WriteType(markdown, page.RequestType);
		_ = markdown.AppendLine();
	}

	private static void WriteResponses(StringBuilder markdown, OperationPageModel page, string apiBaseUrl)
	{
		if (page.Responses.Count == 0)
			return;

		var single = page.Responses.Count == 1;
		ApiCommonMark.Heading(markdown, 2, single ? "Response" : "Responses");
		foreach (var response in page.Responses)
		{
			if (!single)
			{
				var description = string.IsNullOrEmpty(response.Response.Description) ? "" : $" {response.Response.Description}";
				ApiCommonMark.Heading(markdown, 4, $"`{response.StatusCode}`{description}");
			}

			foreach (var content in response.Contents)
			{
				if (!single)
					ApiCommonMark.Paragraph(markdown, $"Content-Type: `{content.ContentType}`");
				if (content.Properties is not null)
					ApiPropertyMarkdown.WriteList(markdown, content.Properties, apiBaseUrl);
				else if (content.ArrayItemProperties is not null)
				{
					ApiPropertyMarkdown.WriteType(markdown, content.Type);
					ApiPropertyMarkdown.WriteList(markdown, content.ArrayItemProperties, apiBaseUrl);
				}
				else
					ApiPropertyMarkdown.WriteType(markdown, content.Type);
			}

			WriteHeaders(markdown, response, apiBaseUrl);
		}
	}

	private static void WriteHeaders(StringBuilder markdown, ApiResponse response, string apiBaseUrl)
	{
		if (response.Headers.Count == 0)
			return;

		ApiCommonMark.Heading(markdown, 5, "Response Headers");
		foreach (var header in response.Headers)
		{
			var type = header.Type is null ? "" : $" ({header.Type.Text})";
			var flags = header.Header?.Required == true ? " — required" : "";
			if (header.Header?.Deprecated == true)
				flags += " — deprecated";
			_ = markdown.AppendLine($"- `{header.Name}`{type}{flags}");
			var description = ApiMarkdown.Prepare(header.Header?.Description, apiBaseUrl);
			if (!string.IsNullOrWhiteSpace(description))
				_ = markdown.AppendLine($"  {description.TrimEnd()}");
		}

		_ = markdown.AppendLine();
	}

	private static void WriteCodeSamples(StringBuilder markdown, OperationPageModel page)
	{
		if (page.CodeSamples.Count == 0)
			return;

		ApiCommonMark.Heading(markdown, 2, "Code Examples");
		foreach (var sample in page.CodeSamples)
		{
			ApiCommonMark.Heading(markdown, 4, sample.Language);
			ApiCommonMark.Fence(markdown, sample.Language.ToLowerInvariant(), sample.Source);
		}
	}

	private static void WriteExamples(
		StringBuilder markdown,
		string heading,
		bool show,
		IReadOnlyList<ExampleDisplay> examples,
		string apiBaseUrl
	)
	{
		if (!show || examples.Count == 0)
			return;

		ApiCommonMark.Heading(markdown, 2, heading);
		foreach (var example in examples)
		{
			ApiCommonMark.Heading(markdown, 4, example.Title);
			ApiCommonMark.Prepared(markdown, example.DescriptionMarkdown, apiBaseUrl);
			if (!string.IsNullOrEmpty(example.ExternalValue))
				ApiCommonMark.Paragraph(markdown, ApiCommonMark.Link(example.ExternalValue, example.ExternalValue));
			ApiCommonMark.Fence(markdown, "json", example.JsonValue);
		}
	}
}
