// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Types;
using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Infrastructure;

internal readonly record struct ApiPageFrontMatter(string Title, string? Description, string Url, string? Product);

/// <summary>
/// Writes LLM-aligned YAML plus the OKF concept fields <c>type</c> and <c>resource</c>.
/// </summary>
internal static class ApiMarkdownFrontMatter
{
	public static string Wrap(string body, INavigationItem current, ApiRenderContext context, IApiModel page)
	{
		var content = StripLeadingFrontMatter(body).TrimStart();
		return Write(content, Collect(content, current, context, page));
	}

	public static string Write(string body, ApiPageFrontMatter meta)
	{
		var markdown = new StringBuilder();
		WriteYaml(markdown, meta);
		_ = markdown.Append(body);
		if (body.Length > 0 && body[^1] != '\n')
			_ = markdown.AppendLine();
		return markdown.ToString();
	}

	public static string StripLeadingFrontMatter(string markdown)
	{
		if (!markdown.StartsWith("---", StringComparison.Ordinal))
			return markdown;

		var end = markdown.IndexOf("\n---", 3, StringComparison.Ordinal);
		if (end < 0)
			return markdown;

		var after = end + 4;
		if (after < markdown.Length && markdown[after] == '\r')
			after++;
		if (after < markdown.Length && markdown[after] == '\n')
			after++;
		return markdown[after..];
	}

	internal static ApiPageFrontMatter Collect(string body, INavigationItem current, ApiRenderContext context, IApiModel page)
	{
		var title = Heading(body) ?? current.NavigationTitle;
		return new(
			title,
			ResolveDescription(page, context) ?? FirstParagraph(body),
			CanonicalUrl(context, current),
			context.Product?.DisplayName
		);
	}

	private static void WriteYaml(StringBuilder markdown, ApiPageFrontMatter meta)
	{
		_ = markdown.AppendLine("---");
		_ = markdown.AppendLine("type: api");
		_ = markdown.AppendLine($"title: {meta.Title}");
		if (!string.IsNullOrWhiteSpace(meta.Description))
			_ = markdown.AppendLine($"description: {meta.Description}");

		_ = markdown.AppendLine($"url: {meta.Url}");
		_ = markdown.AppendLine($"resource: {meta.Url}");
		WriteList(markdown, "products", ProductItems(meta.Product));
		_ = markdown.AppendLine("---");
		_ = markdown.AppendLine();
	}

	private static void WriteList(StringBuilder markdown, string key, IReadOnlyList<string> items)
	{
		if (items.Count == 0)
			return;

		_ = markdown.AppendLine($"{key}:");
		foreach (var item in items)
			_ = markdown.AppendLine($"  - {item}");
	}

	private static IReadOnlyList<string> ProductItems(string? product) => string.IsNullOrWhiteSpace(product) ? [] : [product];

	private static string CanonicalUrl(ApiRenderContext context, INavigationItem current)
	{
		var path = current.Url.TrimEnd('/');
		if (path.Length == 0)
			path = "/";

		return context.BuildContext.CanonicalBaseUrl is { } baseUrl ? new Uri(baseUrl, path).ToString().TrimEnd('/') : path;
	}

	private static string? ResolveDescription(IApiModel page, ApiRenderContext context)
	{
		var apiBaseUrl = context.CurrentNavigation.NavigationRoot.Url;
		return page switch
		{
			ApiCatalog => "API products in this documentation set.",
			ApiLanding => FirstLine(ApiMarkdown.Prepare(context.Model.Info?.Description, apiBaseUrl)),
			ApiTag tag => FirstLine(ApiMarkdown.Prepare(TagDescription(tag, context), apiBaseUrl)),
			ApiOperation operation => FirstLine(ApiMarkdown.Prepare(OperationDescription(operation, context), apiBaseUrl)),
			ApiSchema schema => FirstLine(ApiMarkdown.Prepare(schema.Schema.Description, apiBaseUrl)),
			_ => null
		};
	}

	private static string? TagDescription(ApiTag tag, ApiRenderContext context) =>
		context.TagSupplemental.TryGetValue(tag.Name, out var doc) ? doc.DescriptionOr(tag.Description) : tag.Description;

	private static string? OperationDescription(ApiOperation operation, ApiRenderContext context)
	{
		var spec = operation.Operation.Description;
		if (
			operation.Operation.OperationId is { Length: > 0 } operationId
			&& context.OperationSupplemental.TryGetValue(operationId, out var doc)
		)
			return doc.DescriptionOr(spec);

		return spec;
	}

	private static string? Heading(string markdown)
	{
		using var reader = new StringReader(markdown);
		while (reader.ReadLine() is { } line)
		{
			if (line.StartsWith("# ", StringComparison.Ordinal))
				return line[2..].Trim();
		}

		return null;
	}

	private static string? FirstParagraph(string markdown)
	{
		using var reader = new StringReader(markdown);
		var buffer = new StringBuilder();
		while (reader.ReadLine() is { } line)
		{
			if (line.StartsWith('#'))
			{
				if (buffer.Length > 0)
					break;
				continue;
			}

			if (string.IsNullOrWhiteSpace(line))
			{
				if (buffer.Length > 0)
					break;
				continue;
			}

			if (buffer.Length == 0 && IsSkippableLead(line))
				continue;

			if (buffer.Length > 0)
				_ = buffer.Append(' ');
			_ = buffer.Append(line.Trim());
		}

		return buffer.Length == 0 ? null : buffer.ToString();
	}

	private static bool IsSkippableLead(string line) =>
		line.StartsWith("- ", StringComparison.Ordinal)
			|| line.StartsWith("* ", StringComparison.Ordinal)
			|| line.StartsWith('`')
			|| line.StartsWith("Availability:", StringComparison.Ordinal)
			|| line.Equals("deprecated", StringComparison.OrdinalIgnoreCase)
			|| line.Equals("Beta", StringComparison.Ordinal);

	private static string? FirstLine(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return null;

		using var reader = new StringReader(text.Trim());
		return reader.ReadLine()?.Trim();
	}
}
