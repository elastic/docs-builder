// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.ApiExplorer.Templates;

/// <summary>
/// Processes API landing page templates using standard markdown rendering.
/// </summary>
public class TemplateProcessor(IMarkdownStringRenderer markdownRenderer, IFileSystem fileSystem)
{
	private readonly IMarkdownStringRenderer _markdownRenderer = markdownRenderer;
	private readonly IFileSystem _fileSystem = fileSystem;

	/// <summary>
	/// Processes a template file for a specific API configuration.
	/// </summary>
	public async Task<string> ProcessTemplateAsync(
		ResolvedApiConfiguration apiConfig,
		CancellationToken cancellationToken = default)
	{
		if (!apiConfig.HasCustomTemplate || apiConfig.TemplateFile == null)
			return string.Empty;

		// Read template content
		var templateContent = await _fileSystem.File.ReadAllTextAsync(apiConfig.TemplateFile.FullName, cancellationToken);

		// Only throw if cancellation was requested before we started expensive operations
		// This allows completion of in-flight rendering while respecting explicit cancellation
		if (cancellationToken.IsCancellationRequested)
		{
			// Add small grace period for operations that are nearly complete
			await Task.Delay(TimeSpan.FromMilliseconds(10), CancellationToken.None);
			cancellationToken.ThrowIfCancellationRequested();
		}

		// Template uses standard substitutions and directives - render directly.
		return _markdownRenderer.Render(templateContent, apiConfig.TemplateFile);
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
	public static TemplateProcessor Create(IMarkdownStringRenderer markdownRenderer, IFileSystem fileSystem) => new(markdownRenderer, fileSystem);
}
