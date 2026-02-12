// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Elastic.Documentation.Assembler.Mcp.Responses;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Assembler.Mcp;

[McpServerToolType]
public partial class ContentTypeTools(ContentTypeProvider provider)
{
	private static readonly List<ContentTypeSummary> ContentTypes =
	[
		new(
			"overview",
			"Conceptual information that helps users understand a feature, product, or concept. Answers: What is it? How does it work? How does it bring value?",
			"Use when you need to explain what something is and why it matters, clarify how components relate to each other, or help users choose between options.",
			"Do not use for step-by-step instructions (use how-to), hands-on learning experiences (use tutorial), or fixing specific problems (use troubleshooting)."
		),
		new(
			"how-to",
			"A short set of instructions to accomplish a specific task, like a cooking recipe. Focuses on a single, self-contained task with minimal explanation.",
			"Use when users need to complete a specific, discrete task with clear steps and a defined outcome.",
			"Do not use for broader learning objectives (use tutorial), conceptual explanations (use overview), or fixing problems (use troubleshooting)."
		),
		new(
			"tutorial",
			"A comprehensive, hands-on learning experience that guides users through completing a meaningful task from start to finish. Chains multiple how-to guides with explanatory context.",
			"Use when you need to teach a broader concept or workflow, guide users through an end-to-end experience, or combine multiple tasks with learning objectives.",
			"Do not use for single discrete tasks (use how-to), pure conceptual content (use overview), or problem resolution (use troubleshooting)."
		),
		new(
			"troubleshooting",
			"Helps users fix specific problems. Intentionally narrow in scope (one issue per page), problem-driven, and focused on fast resolution.",
			"Use when users encounter a specific, repeatable problem with identifiable symptoms and a known resolution or workaround.",
			"Do not use for teaching features (use tutorial), explaining systems (use overview), listing configuration options (use reference), or general best practices."
		),
		new(
			"changelog",
			"YAML-based entries describing product changes (features, enhancements, bug fixes, breaking changes, deprecations, etc.). Building blocks of release notes.",
			"Use when documenting a product change in a pull request that should appear in release notes.",
			"Do not use for documentation pages. Changelogs are structured YAML data, not Markdown content."
		)
	];

	/// <summary>
	/// Lists all available Elastic Docs content types.
	/// </summary>
	[McpServerTool, Description(
		"Lists all Elastic Docs content types with descriptions and guidance on when to use each. " +
		"Use this to determine the right content type before creating a new documentation page.")]
	public string ListContentTypes()
	{
		try
		{
			return JsonSerializer.Serialize(
				new ListContentTypesResponse(ContentTypes.Count, ContentTypes),
				McpJsonContext.Default.ListContentTypesResponse);
		}
		catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException and not StackOverflowException)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	/// <summary>
	/// Generates a template for a specific content type.
	/// </summary>
	[McpServerTool, Description(
		"Generates a ready-to-use documentation template for a specific Elastic Docs content type. " +
		"Returns a Markdown template (or YAML for changelogs) with correct frontmatter and structure. " +
		"Optionally pre-fills title, description, and product fields.")]
	public async Task<string> GenerateTemplate(
		[Description("The content type: 'overview', 'how-to', 'tutorial', 'troubleshooting', or 'changelog'")] string contentType,
		[Description("Optional: pre-fill the page title or changelog title")] string? title = null,
		[Description("Optional: pre-fill the frontmatter description")] string? description = null,
		[Description("Optional: pre-fill the product field")] string? product = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			if (!ContentTypeProvider.IsValidContentType(contentType))
			{
				return JsonSerializer.Serialize(
					new ErrorResponse(
						$"Unknown content type '{contentType}'.",
						[$"Valid content types: {string.Join(", ", ContentTypeProvider.ValidContentTypes)}"]),
					McpJsonContext.Default.ErrorResponse);
			}

			var (template, source) = await provider.GetTemplateAsync(contentType, cancellationToken);

			template = ApplyTemplateSubstitutions(template, contentType, title, description, product);

			return JsonSerializer.Serialize(
				new GenerateTemplateResponse(contentType, template, source),
				McpJsonContext.Default.GenerateTemplateResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	/// <summary>
	/// Gets authoring and evaluation guidelines for a content type.
	/// </summary>
	[McpServerTool, Description(
		"Returns detailed authoring and evaluation guidelines for a specific Elastic Docs content type. " +
		"Includes required elements checklist, recommended sections, best practices, and anti-patterns. " +
		"Use this to write new content following the guidelines, or to evaluate existing content against them.")]
	public string GetContentTypeGuidelines(
		[Description("The content type: 'overview', 'how-to', 'tutorial', 'troubleshooting', or 'changelog'")] string contentType)
	{
		try
		{
			if (!ContentTypeProvider.IsValidContentType(contentType))
			{
				return JsonSerializer.Serialize(
					new ErrorResponse(
						$"Unknown content type '{contentType}'.",
						[$"Valid content types: {string.Join(", ", ContentTypeProvider.ValidContentTypes)}"]),
					McpJsonContext.Default.ErrorResponse);
			}

			var guidelines = provider.GetGuidelines(contentType);

			return JsonSerializer.Serialize(
				new ContentTypeGuidelinesResponse(contentType, guidelines),
				McpJsonContext.Default.ContentTypeGuidelinesResponse);
		}
		catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException and not StackOverflowException)
		{
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	private static string ApplyTemplateSubstitutions(string template, string contentType, string? title, string? description, string? product)
	{
		if (contentType == "changelog")
			return ApplyChangelogSubstitutions(template, title, description, product);

		return ApplyMarkdownSubstitutions(template, title, description, product);
	}

	private static string ApplyMarkdownSubstitutions(string template, string? title, string? description, string? product)
	{
		if (title is not null)
			template = TitlePattern().Replace(template, $"# {title}", 1);

		if (description is not null)
			template = DescriptionPattern().Replace(template, $"description: \"{description}\"", 1);

		if (product is not null)
			template = ProductPattern().Replace(template, $"product: {product}", 1);

		return template;
	}

	private static string ApplyChangelogSubstitutions(string template, string? title, string? description, string? product)
	{
		if (title is not null)
			template = ChangelogTitlePattern().Replace(template, $"title: {title}", 1);

		if (description is not null)
			template = ChangelogDescriptionPattern().Replace(template, $"description: |\n  {description}", 1);

		if (product is not null)
			template = ChangelogProductPattern().Replace(template, $"- product: {product}", 1);

		return template;
	}

	// Markdown template patterns
	[GeneratedRegex(@"^# \[.*?\]", RegexOptions.Multiline)]
	private static partial Regex TitlePattern();

	[GeneratedRegex(@"^description: "".*?""", RegexOptions.Multiline)]
	private static partial Regex DescriptionPattern();

	[GeneratedRegex(@"^product:\s*$", RegexOptions.Multiline)]
	private static partial Regex ProductPattern();

	// Changelog template patterns
	[GeneratedRegex(@"^title:\s*$", RegexOptions.Multiline)]
	private static partial Regex ChangelogTitlePattern();

	[GeneratedRegex(@"^description: \|.*$", RegexOptions.Multiline)]
	private static partial Regex ChangelogDescriptionPattern();

	[GeneratedRegex(@"^\s+- product:\s*$", RegexOptions.Multiline)]
	private static partial Regex ChangelogProductPattern();
}
