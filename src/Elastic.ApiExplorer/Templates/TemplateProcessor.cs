// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Toc;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Templates;

/// <summary>
/// Processes API landing page templates by replacing directives with generated content.
/// </summary>
public class TemplateProcessor(IMarkdownStringRenderer markdownRenderer)
{
	private readonly IMarkdownStringRenderer _markdownRenderer = markdownRenderer;

	/// <summary>
	/// Processes a template file for a specific API configuration.
	/// </summary>
	public async Task<string> ProcessTemplateAsync(
		ResolvedApiConfiguration apiConfig,
		string urlPathPrefix,
		CancellationToken cancellationToken = default)
	{
		if (!apiConfig.HasCustomTemplate || apiConfig.TemplateFile == null)
			return string.Empty;

		// Read template content
		var templateContent = await File.ReadAllTextAsync(apiConfig.TemplateFile.FullName, cancellationToken);

		// Load OpenAPI documents for this configuration
		var openApiDocuments = new Dictionary<string, OpenApiDocument>();
		foreach (var specFile in apiConfig.SpecFiles)
		{
			try
			{
				var document = await OpenApiReader.Create(specFile);
				if (document != null)
				{
					// Use the spec file name (without extension) as the key
					var specName = Path.GetFileNameWithoutExtension(specFile.Name);
					openApiDocuments[specName] = document;
				}
			}
			catch (Exception ex)
			{
				// Log error but continue processing other specs
				// In a real implementation, you'd use proper logging here
				Console.WriteLine($"Failed to load OpenAPI spec {specFile.Name}: {ex.Message}");
			}
		}

		// Process directives
		var processedContent = await ProcessDirectivesAsync(
			templateContent,
			openApiDocuments,
			urlPathPrefix,
			apiConfig.ProductKey,
			cancellationToken);

		// Render markdown to HTML
		var html = _markdownRenderer.Render(processedContent, apiConfig.TemplateFile);

		return html;
	}

	/// <summary>
	/// Processes all directives in the template content and replaces them with generated HTML.
	/// </summary>
#pragma warning disable IDE0060 // Remove unused parameter - reserved for future async operations
	private async Task<string> ProcessDirectivesAsync(
		string templateContent,
		Dictionary<string, OpenApiDocument> openApiDocuments,
		string urlPathPrefix,
		string productKey,
		CancellationToken cancellationToken = default)
#pragma warning restore IDE0060
	{
		// Parse all directives
		var directives = TemplateDirectiveParser.ParseDirectives(templateContent);
		if (directives.Count == 0)
			return templateContent;

		// Create directive renderer
		var renderer = new DirectiveRenderer(openApiDocuments, urlPathPrefix, productKey);

		// Replace directives from end to beginning to preserve indices
		var result = new StringBuilder(templateContent);
		foreach (var directive in directives.OrderByDescending(d => d.StartIndex))
		{
			var renderedContent = renderer.RenderDirective(directive);
			_ = result.Remove(directive.StartIndex, directive.Length);
			_ = result.Insert(directive.StartIndex, renderedContent);
		}

		return result.ToString();
	}
}

/// <summary>
/// Factory for creating template processors with proper dependencies.
/// </summary>
public class TemplateProcessorFactory
{
	/// <summary>
	/// Creates a template processor with the required dependencies.
	/// </summary>
	public static TemplateProcessor Create(IMarkdownStringRenderer markdownRenderer) => new(markdownRenderer);
}
