// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Templates;

/// <summary>
/// Parses template directives in API landing page templates.
/// Supports directives like {{% api-operations-nav from="spec.json" tag="search" %}}
/// </summary>
public partial class TemplateDirectiveParser
{
	[GeneratedRegex(@"\{\{%\s*([\w-]+)\s*([^%}]*)\s*%\}\}", RegexOptions.Multiline)]
	private static partial Regex DirectiveRegex();

	[GeneratedRegex(@"(\w+)=[""']([^""']+)[""']")]
	private static partial Regex AttributeRegex();

	public record ParsedDirective(
		string DirectiveName,
		Dictionary<string, string> Attributes,
		int StartIndex,
		int Length
	);

	/// <summary>
	/// Parses all directives from a template markdown content.
	/// </summary>
	public static List<ParsedDirective> ParseDirectives(string templateContent)
	{
		var directives = new List<ParsedDirective>();
		var matches = DirectiveRegex().Matches(templateContent);

		foreach (Match match in matches)
		{
			var directiveName = match.Groups[1].Value;
			var attributesText = match.Groups[2].Value;
			var attributes = ParseAttributes(attributesText);

			directives.Add(new ParsedDirective(
				directiveName,
				attributes,
				match.Index,
				match.Length
			));
		}

		return directives;
	}

	/// <summary>
	/// Parses attributes from a directive string like 'from="spec.json" tag="search"'
	/// </summary>
	private static Dictionary<string, string> ParseAttributes(string attributesText)
	{
		var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var matches = AttributeRegex().Matches(attributesText);

		foreach (Match match in matches)
		{
			var key = match.Groups[1].Value;
			var value = match.Groups[2].Value;
			attributes[key] = value;
		}

		return attributes;
	}
}

/// <summary>
/// Generates HTML content for template directives based on OpenAPI documents.
/// </summary>
public class DirectiveRenderer(
	Dictionary<string, OpenApiDocument> openApiDocuments,
	string urlPathPrefix,
	string apiUrlSuffix)
{
	private readonly Dictionary<string, OpenApiDocument> _openApiDocuments = openApiDocuments;
	private readonly string _urlPathPrefix = urlPathPrefix;
	private readonly string _apiUrlSuffix = apiUrlSuffix;

	/// <summary>
	/// Renders a directive to HTML content.
	/// </summary>
	public string RenderDirective(TemplateDirectiveParser.ParsedDirective directive) => directive.DirectiveName.ToLowerInvariant() switch
	{
		"api-operations-nav" => RenderApiOperationsNav(directive.Attributes),
		_ => $"<!-- Unknown directive: {directive.DirectiveName} -->"
	};

	/// <summary>
	/// Renders the api-operations-nav directive which generates navigation links to API operations.
	/// Supports attributes: from="spec-name", tag="tag-filter", exclude="tag1,tag2"
	/// </summary>
	private string RenderApiOperationsNav(Dictionary<string, string> attributes)
	{
		var fromSpec = attributes.GetValueOrDefault("from");
		var tagFilter = attributes.GetValueOrDefault("tag");
		var excludeFilter = attributes.GetValueOrDefault("exclude");
		var excludeTags = excludeFilter?.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(t => t.Trim())
			.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

		// Determine which specs to include
		var specsToProcess = new List<(string Name, OpenApiDocument Document)>();
		if (!string.IsNullOrEmpty(fromSpec))
		{
			// Specific spec requested
			if (_openApiDocuments.TryGetValue(fromSpec, out var specificDoc))
				specsToProcess.Add((fromSpec, specificDoc));
		}
		else
		{
			// All specs
			specsToProcess.AddRange(_openApiDocuments.Select(kvp => (kvp.Key, kvp.Value)));
		}

		if (specsToProcess.Count == 0)
			return "<!-- No API specifications found -->";

		var html = new List<string>
		{
			"<div class=\"api-operations-nav\">"
		};

		foreach (var (specName, document) in specsToProcess)
		{
			// Group operations by tag
			var operationsByTag = document.Paths
				.SelectMany(p => (p.Value.Operations ?? []).Select(op => new
				{
					Path = p.Key,
					Method = op.Key.ToString().ToUpperInvariant(),
					Operation = op.Value,
					Tag = op.Value.Tags?.FirstOrDefault()?.Reference.Id ?? "untagged"
				}))
				.Where(op => string.IsNullOrEmpty(tagFilter) || op.Tag.Equals(tagFilter, StringComparison.OrdinalIgnoreCase))
				.Where(op => !excludeTags.Contains(op.Tag))
				.GroupBy(op => op.Tag)
				.OrderBy(g => g.Key);

			foreach (var tagGroup in operationsByTag)
			{
				html.Add($"<h3>{EscapeHtml(tagGroup.Key)}</h3>");
				html.Add("<ul>");

				foreach (var operation in tagGroup.OrderBy(op => op.Path))
				{
					var operationUrl = $"{_urlPathPrefix}/api/{_apiUrlSuffix}{operation.Path}";
					var operationTitle = operation.Operation.Summary ?? $"{operation.Method} {operation.Path}";

					html.Add($"<li><a href=\"{EscapeHtml(operationUrl)}\">{EscapeHtml(operationTitle)}</a></li>");
				}

				html.Add("</ul>");
			}
		}

		html.Add("</div>");
		return string.Join(Environment.NewLine, html);
	}

	private static string EscapeHtml(string text) => text
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;")
			.Replace("\"", "&quot;")
			.Replace("'", "&#39;");
}
