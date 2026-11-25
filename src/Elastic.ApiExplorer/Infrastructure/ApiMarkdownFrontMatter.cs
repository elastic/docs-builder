// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Structural;
using Elastic.ApiExplorer.Supplemental;
using Elastic.ApiExplorer.Topics;
using Elastic.ApiExplorer.Types;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>What the product landing shows in its right rail, so agents reading the Markdown get it too.</summary>
internal sealed record ApiRailFacts(string? Version, string? License, IReadOnlyList<string> Servers, string? SpecJson, string? SpecYaml);

internal readonly record struct ApiPageFrontMatter(
	string Title,
	string? Description,
	string Url,
	string? Product,
	ApiRailFacts? Facts = null,
	string? Resource = null
);

/// <summary>
/// Writes LLM-aligned YAML plus the OKF concept fields <c>type</c> and <c>resource</c>. The <c>resource</c> is the page's
/// source when it has one, such as an operation in the spec, and the page's own URL otherwise.
/// </summary>
internal static class ApiMarkdownFrontMatter
{
	public static string Wrap(string body, INavigationItem current, ApiRenderContext context, IApiModel page)
	{
		var content = StripLeadingFrontMatter(body).TrimStart();
		content = ApiMarkdown.CanonicalizeLinks(content, context.BuildContext.CanonicalBaseUrl);
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

	public static string StripLeadingFrontMatter(string markdown) => ApiSupplementalDoc.ExtractFrontMatter(markdown).Content;

	internal static ApiPageFrontMatter Collect(string body, INavigationItem current, ApiRenderContext context, IApiModel page)
	{
		// Front matter is plain text: the H1 is Markdown, so an escaped name such as `HTTP \<code\> responses` reads as written. It is
		// read as a heading, so `1. List roles` is not taken for a list.
		var title = Plain(Heading(body) is { } heading ? $"# {heading}" : null) ?? current.NavigationTitle;
		return new(
			title,
			ResolveDescription(page, context, body, current.NavigationTitle),
			context.CanonicalUrl(current.Url),
			context.Product?.DisplayName,
			page is ApiLanding ? CollectFacts(context) : null,
			page is ApiOperation operation
				? SpecResource(operation, context.CurrentNavigation.NavigationRoot.Url, context.BuildContext.CanonicalBaseUrl)
				: null
		);
	}

	/// <summary>
	/// Where an operation page comes from: the operation in its tree's published spec JSON, as a JSON pointer such as
	/// <c>.../api/elasticsearch.json#/paths/~1_search/post</c>.
	/// </summary>
	internal static string? SpecResource(ApiOperation operation, string treeRootUrl, Uri? canonicalBaseUrl)
	{
		var path = operation.Route.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
		// A path item that is a $ref serializes as that reference and has no operation member, so the pointer stops at the path item.
		var method = operation.Path is OpenApiPathItemReference ? "" : $"/{operation.OperationType.Method.ToLowerInvariant()}";
		var spec = ApiOutputPaths.JsonUrl(treeRootUrl);
		return UrlPath.MakeAbsolute(canonicalBaseUrl, $"{spec}#/paths/{Uri.EscapeDataString(path)}{method}");
	}

	private static ApiRailFacts CollectFacts(ApiRenderContext context)
	{
		var info = context.Model.Info;
		var pageUrl = context.CurrentNavigation.Url;
		var canonical = context.BuildContext.CanonicalBaseUrl;
		return new ApiRailFacts(
			info?.Version,
			info?.License?.Name,
			[.. StructuralViewModel.ReadServers(context.Model).Select(server => server.Url)],
			UrlPath.MakeAbsolute(canonical, ApiOutputPaths.JsonUrl(pageUrl)),
			UrlPath.MakeAbsolute(canonical, ApiOutputPaths.YamlUrl(pageUrl))
		);
	}

	private static void WriteYaml(StringBuilder markdown, ApiPageFrontMatter meta)
	{
		_ = markdown.AppendLine("---");
		_ = markdown.AppendLine("type: api");
		_ = markdown.AppendLine($"title: {YamlScalar(meta.Title)}");
		if (!string.IsNullOrWhiteSpace(meta.Description))
			_ = markdown.AppendLine($"description: {YamlScalar(meta.Description)}");

		_ = markdown.AppendLine($"url: {meta.Url}");
		_ = markdown.AppendLine($"resource: {meta.Resource ?? meta.Url}");
		if (!string.IsNullOrWhiteSpace(meta.Product))
		{
			_ = markdown.AppendLine("products:");
			_ = markdown.AppendLine($"  - {meta.Product}");
		}

		if (meta.Facts is { } facts)
			WriteFacts(markdown, facts);

		_ = markdown.AppendLine("---");
		_ = markdown.AppendLine();
	}

	private static void WriteFacts(StringBuilder markdown, ApiRailFacts facts)
	{
		WriteLine(markdown, "version", facts.Version, quote: true);
		WriteLine(markdown, "license", facts.License, quote: true);
		if (facts.Servers.Count > 0)
		{
			_ = markdown.AppendLine("servers:");
			foreach (var server in facts.Servers)
				_ = markdown.AppendLine($"  - {YamlScalar(server)}");
		}

		if (string.IsNullOrEmpty(facts.SpecJson) && string.IsNullOrEmpty(facts.SpecYaml))
			return;

		_ = markdown.AppendLine("spec:");
		WriteLine(markdown, "  json", facts.SpecJson, quote: false);
		WriteLine(markdown, "  yaml", facts.SpecYaml, quote: false);
	}

	private static void WriteLine(StringBuilder markdown, string key, string? value, bool quote)
	{
		if (!string.IsNullOrEmpty(value))
			_ = markdown.AppendLine($"{key}: {(quote ? YamlScalar(value) : value)}");
	}

	// Quoted always: a version like 1.2 would otherwise parse as a number, a server like {scheme}://x as a flow mapping.
	// Mirrors OkfMarkdownExporter.YamlScalar; Elastic.ApiExplorer cannot reference Elastic.Markdown.
	private static string YamlScalar(string value) =>
		$"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t")}\"";

	private static string? ResolveDescription(IApiModel page, ApiRenderContext context, string body, string navigationTitle) =>
		AuthoredDescription(page, context, body) ?? ApiFallbackDescription.For(page, context, navigationTitle);

	private static string? AuthoredDescription(IApiModel page, ApiRenderContext context, string body)
	{
		var apiBaseUrl = context.CurrentNavigation.NavigationRoot.Url;
		return page switch
		{
			ApiCatalog => ApiCatalog.PageDescription,
			ApiLanding => ApiSeoDescription.Excerpt(ApiMarkdown.Prepare(context.LandingDescription, apiBaseUrl)),
			ApiTag tag => TagSummary(tag, context),
			ApiOperation operation => OperationDescriptionLine(operation, context, apiBaseUrl),
			ApiStructuralPage structural => ApiSeoDescription.Excerpt(ApiMarkdown.Prepare(structural.Intro, apiBaseUrl)),
			ApiSchema schema => FirstLine(ApiMarkdown.Prepare(schema.Schema.Description, apiBaseUrl)),
			ApiTopicPage topic => FirstLine(ApiMarkdown.Prepare(topic.Content, apiBaseUrl)),
			SimpleMarkdownNavigationItem => Plain(ApiSeoDescription.FirstParagraph(body)),
			_ => null
		};
	}

	/// <summary>
	/// The first line of the operation's own text once its HTML comments are gone, as the HTML page reads it, or null when nothing a reader
	/// would see is left. The page then takes the generated sentence.
	/// </summary>
	private static string? OperationDescriptionLine(ApiOperation operation, ApiRenderContext context, string apiBaseUrl)
	{
		var own = ApiSeoDescription.SkipPathPreamble(OperationDescription(operation, context));
		if (!string.IsNullOrWhiteSpace(own))
			return FirstLine(ApiMarkdown.Prepare(own, apiBaseUrl) is { } prepared ? ApiSeoDescription.WithoutComments(prepared) : null);

		// A summary is plain text in OpenAPI, so it stays as written.
		return OpenApiExtensionReader.GetSummary(operation.Operation)?.Trim() is { Length: > 0 } summary ? summary : null;
	}

	/// <summary>The one-line description of a group, from its supplemental description when it has one: what its meta description and its card on the product landing page say.</summary>
	internal static string? TagSummary(ApiTag tag, ApiRenderContext context) =>
		ApiSeoDescription.Excerpt(ApiMarkdown.Prepare(TagDescription(tag, context), context.CurrentNavigation.NavigationRoot.Url));

	/// <summary>The description a group shows: its supplemental one when it has one, else the spec's.</summary>
	internal static string? TagDescription(ApiTag tag, ApiRenderContext context) =>
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

	/// <summary>The first line of Markdown <paramref name="text"/>, as plain text.</summary>
	private static string? FirstLine(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return null;

		using var reader = new StringReader(text.Trim());
		return Plain(reader.ReadLine());
	}

	private static string? Plain(string? markdown) =>
		string.IsNullOrWhiteSpace(markdown) ? null : ApiMarkdown.ToPlainText(markdown) is { Length: > 0 } plain ? plain : null;
}
