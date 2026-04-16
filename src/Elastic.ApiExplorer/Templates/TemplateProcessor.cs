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

		// The markdown pipeline uses DisableHtml(); raw HTML from directives must not be passed through
		// Markdig, or it is escaped as visible text. Render markdown segments and directive HTML separately,
		// then concatenate.
		var directives = TemplateDirectiveParser.ParseDirectives(templateContent);
		if (directives.Count == 0)
			return _markdownRenderer.Render(templateContent, apiConfig.TemplateFile);

		var directiveRenderer = new DirectiveRenderer(openApiDocuments, urlPathPrefix, apiConfig.ProductKey);
		var sorted = directives.OrderBy(d => d.StartIndex).ToList();
		var html = new StringBuilder();
		var lastEnd = 0;
		foreach (var directive in sorted)
		{
			if (directive.StartIndex > lastEnd)
			{
				var mdSegment = templateContent.Substring(lastEnd, directive.StartIndex - lastEnd);
				if (mdSegment.Length > 0)
					_ = html.Append(_markdownRenderer.Render(mdSegment, apiConfig.TemplateFile));
			}

			_ = html.Append(directiveRenderer.RenderDirective(directive));
			lastEnd = directive.StartIndex + directive.Length;
		}

		if (lastEnd < templateContent.Length)
		{
			var tail = templateContent.Substring(lastEnd);
			if (tail.Length > 0)
				_ = html.Append(_markdownRenderer.Render(tail, apiConfig.TemplateFile));
		}

		return html.ToString();
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
