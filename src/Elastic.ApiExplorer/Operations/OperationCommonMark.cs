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
	public static string Write(ApiOperation apiOperation, OperationPageModel page, ApiRenderContext context)
	{
		var markdown = new StringBuilder();
		var apiBaseUrl = context.CurrentNavigation.NavigationRoot.Url;
		var operation = apiOperation.Operation;
		var title = operation.Summary ?? apiOperation.ApiName;
		ApiCommonMark.Heading(markdown, 1, title);
		WriteBadges(markdown, operation, page);
		WriteServers(markdown, page);
		WritePaths(markdown, page);
		WritePathParameters(markdown, page, apiBaseUrl);
		WriteDescription(markdown, page, apiBaseUrl);
		WriteAuthorization(markdown, page, apiBaseUrl);
		WriteParameterSections(markdown, page, apiBaseUrl);
		WriteRequestBody(markdown, apiOperation, page, apiBaseUrl);
		WriteResponses(markdown, page, apiBaseUrl);
		WriteCodeSamples(markdown, page);
		WriteExamples(markdown, "Request examples", page.ShowRequestExamples, page.RequestExamples, apiBaseUrl);
		WriteExamples(markdown, "Response examples", page.ShowResponseExamples, page.ResponseExamples, apiBaseUrl);
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
		if (page.Availability is null)
			return;

		var texts = page
			.Availability
			.GetApplicabilityItems()
			.SelectMany(item => item.RenderData.PopoverData?.AvailabilityItems ?? [])
			.Select(a => a.Text)
			.ToArray();
		if (texts.Length > 0)
			ApiCommonMark.Paragraph(markdown, $"Availability: {string.Join(", ", texts)}");
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

	private static void WritePaths(StringBuilder markdown, OperationPageModel page)
	{
		ApiCommonMark.Heading(markdown, 2, "Paths");
		foreach (var row in page.Endpoint.Rows)
			_ = markdown.AppendLine($"- {PathLabel(row)}");

		_ = markdown.AppendLine();
	}

	/// <summary><c>`POST` (also `GET`) `/{index}/_search`</c>.</summary>
	public static string PathLabel(EndpointRow row)
	{
		var also = row.AlsoMethods.Count == 0
			? ""
			: $" (also {string.Join(", ", row.AlsoMethods.Select(static m => $"`{m.ToUpperInvariant()}`"))})";
		return $"`{row.Method.ToUpperInvariant()}`{also} `{row.Route}`";
	}

	private static void WritePathParameters(StringBuilder markdown, OperationPageModel page, string apiBaseUrl)
	{
		if (page.PathParameters.Count == 0)
			return;

		ApiCommonMark.Heading(markdown, 4, "Path parameters");
		foreach (var path in page.PathParameters)
		{
			var type = path.Type is null ? "" : $" ({path.Type.Text})";
			var deprecated = path.Deprecated is true ? " — deprecated" : "";
			var optional = path.Optional ? $" — optional, leave it out to use `{page.Endpoint.ShortestRoute}`" : "";
			_ = markdown.AppendLine($"- `{path.Name}`{type}{optional}{deprecated}");
			var description = ApiMarkdown.Prepare(path.DescriptionMarkdown, apiBaseUrl);
			if (!string.IsNullOrWhiteSpace(description))
				_ = markdown.AppendLine($"  {description.TrimEnd()}");
			WriteValues(markdown, path.UnionOptions, path.UnionLabel, path.EnumValues);
		}

		_ = markdown.AppendLine();
	}

	private static void WriteDescription(StringBuilder markdown, OperationPageModel page, string apiBaseUrl)
	{
		var hasDesc = !string.IsNullOrWhiteSpace(page.DescriptionMarkdown);
		if (!hasDesc && page.ExternalDocs is null)
			return;

		ApiCommonMark.Heading(markdown, 2, "Description");

		if (hasDesc)
			ApiCommonMark.Prepared(markdown, page.DescriptionMarkdown, apiBaseUrl);

		if (page.ExternalDocs is { } docs)
			ApiCommonMark.Paragraph(markdown, ApiCommonMark.Link(docs.LinkText, docs.Url));
	}

	private static void WriteAuthorization(StringBuilder markdown, OperationPageModel page, string apiBaseUrl)
	{
		if (page.RequiredPrivileges.Count > 0)
		{
			ApiCommonMark.Heading(markdown, 2, "Required authorization");
			foreach (var row in page.RequiredPrivileges)
			{
				var values = string.Join(row.Join is null ? ", " : $" {row.Join} ", row.Values.Select(static v => $"`{v}`"));
				_ = markdown.AppendLine(
					values.Length == 0 ? $"- {ApiMarkdown.Prepare(row.Label, apiBaseUrl)}" : $"- {row.Label}: {values}"
				);
			}

			_ = markdown.AppendLine();
		}

		if (page.AuthRequirements.Count == 0)
			return;

		ApiCommonMark.Heading(markdown, 2, "Authentication");
		foreach (var requirement in page.AuthRequirements)
			WriteRequirement(markdown, requirement, apiBaseUrl);

		_ = markdown.AppendLine();
	}

	// One scheme is a plain bullet. Several schemes are sent together, so they nest under one bullet that says so.
	internal static void WriteRequirement(StringBuilder markdown, AuthRequirement requirement, string apiBaseUrl)
	{
		var together = requirement.Schemes.Count > 1;
		var indent = together ? "  " : "";
		if (together)
			_ = markdown.AppendLine($"- {requirement.Label} (send all of these)");
		foreach (var scheme in requirement.Schemes)
		{
			var credential = scheme.CredentialExample is null ? "" : $": `{scheme.CredentialExample}`";
			_ = markdown.AppendLine($"{indent}- {scheme.Label}{credential}");
			var description = ApiMarkdown.Prepare(scheme.DescriptionMarkdown, apiBaseUrl);
			if (!string.IsNullOrWhiteSpace(description))
				_ = markdown.AppendLine($"{indent}  {description.Trim().ReplaceLineEndings($"\n{indent}  ")}");
		}
	}

	private static void WriteParameterSections(StringBuilder markdown, OperationPageModel page, string apiBaseUrl)
	{
		foreach (var section in page.ParameterSections)
		{
			ApiCommonMark.Heading(markdown, 2, section.MarkdownHeading);
			ApiPropertyMarkdown.WriteList(markdown, section.Parameters, apiBaseUrl);
			_ = markdown.AppendLine();
		}
	}

	private static void WriteValues(
		StringBuilder markdown,
		IReadOnlyList<UnionBadge> unionOptions,
		string unionLabel,
		IReadOnlyList<string> enumValues
	)
	{
		if (enumValues.Count > 0)
			_ = markdown.AppendLine("  Values: " + string.Join(", ", enumValues.Select(v => $"`{v}`")));
		else if (unionOptions.Count > 0)
			_ = markdown.AppendLine($"  {unionLabel} " + string.Join(" or ", unionOptions.Select(o => $"`{o.Text}`")));
	}

	private static void WriteRequestBody(StringBuilder markdown, ApiOperation apiOperation, OperationPageModel page, string apiBaseUrl)
	{
		if (apiOperation.Operation.RequestBody is null)
			return;

		ApiCommonMark.Heading(markdown, 2, "Request body");
		if (!string.IsNullOrEmpty(page.RequestContentType))
			ApiCommonMark.Paragraph(markdown, $"`{page.RequestContentType}`");
		ApiCommonMark.Prepared(markdown, apiOperation.Operation.RequestBody.Description, apiBaseUrl);
		if (page.RequestRequires is { } requestRequires)
			ApiCommonMark.Paragraph(markdown, ApiPropertyMarkdown.Format(requestRequires));
		if (page.HasRequestBody)
		{
			ApiPropertyMarkdown.WriteList(markdown, page.RequestProperties, apiBaseUrl);
			ApiPropertyMarkdown.WriteVariants(markdown, page.RequestUnionVariants, apiBaseUrl);
		}
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
				ApiCommonMark.Heading(markdown, 4, $"`{response.StatusCode}`");

			ApiCommonMark.Prepared(markdown, response.Response.Description, apiBaseUrl);

			foreach (var content in response.Contents)
			{
				ApiCommonMark.Paragraph(markdown, $"`{content.ContentType}`");
				if (content.Requires is { } requires)
					ApiCommonMark.Paragraph(markdown, ApiPropertyMarkdown.Format(requires));
				if (content.HasBody)
				{
					ApiPropertyMarkdown.WriteList(markdown, content.Properties, apiBaseUrl);
					ApiPropertyMarkdown.WriteVariants(markdown, content.UnionVariants, apiBaseUrl);
				}
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

		ApiCommonMark.Heading(markdown, 5, "Response headers");
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

		ApiCommonMark.Heading(markdown, 2, "Code examples");
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
