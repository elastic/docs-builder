// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Markdown.Myst.Renderers;
using Markdig.Syntax;

namespace Elastic.Markdown.Exporters;

/// <summary>
/// Exports markdown files as LLM-optimized CommonMark using custom renderers
/// </summary>
public class LlmMarkdownExporter : IMarkdownExporter
{

	public ValueTask StartAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public ValueTask StopAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx) => ValueTask.FromResult(true);

	public async ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		// Convert the parsed markdown document to LLM-friendly format using our custom renderers
		var llmMarkdown = ConvertToLlmMarkdown(fileContext.Document, fileContext);

		// Determine output file path
		var outputFile = GetLlmOutputFile(fileContext);

		// Ensure output directory exists
		if (outputFile.Directory is { Exists: false })
			outputFile.Directory.Create();

		// Write LLM markdown with metadata header
		var contentWithMetadata = CreateLlmContentWithMetadata(fileContext, llmMarkdown);
		await fileContext.SourceFile.SourceFile.FileSystem.File.WriteAllTextAsync(
			outputFile.FullName,
			contentWithMetadata,
			Encoding.UTF8,
			ctx
		);

		return true;
	}

	private string ConvertToLlmMarkdown(MarkdownDocument document, MarkdownExportFileContext context)
	{
		using var writer = new StringWriter();

		// Create a new renderer for consistent LLM output with BuildContext for URL transformation
		var renderer = new LlmMarkdownRenderer(writer)
		{
			BuildContext = context.BuildContext
		};

		_ = renderer.Render(document);
		var content = writer.ToString();

		// Apply substitutions to the final content
		content = ApplySubstitutions(content, context);

		return content;
	}

	private IFileInfo GetLlmOutputFile(MarkdownExportFileContext fileContext)
	{
		var source = fileContext.SourceFile.SourceFile;
		var fs = source.FileSystem;
		var defaultOutputFile = fileContext.DefaultOutputFile;

		// Handle both index.md and index.html files (HTML output files)
		var fileName = Path.GetFileNameWithoutExtension(defaultOutputFile.Name);
		if (fileName == "index")
		{
			var root = fileContext.BuildContext.DocumentationOutputDirectory;

			// Root index becomes llm-docs.md
			if (defaultOutputFile.Directory!.FullName == root.FullName)
			{
				return fs.FileInfo.New(Path.Combine(root.FullName, "llm-docs.md"));
			}
			else
			{
				// For index files: /docs/section/index.html -> /docs/section.llm.md
				// This allows users to append .llm.md to any URL path
				var folderName = defaultOutputFile.Directory!.Name;
				return fs.FileInfo.New(Path.Combine(
					defaultOutputFile.Directory!.Parent!.FullName,
					$"{folderName}.md"
				));
			}
		}
		else
		{
			// Regular files: /docs/section/page.html -> /docs/section/page.llm.md
			var directory = defaultOutputFile.Directory!.FullName;
			var baseName = Path.GetFileNameWithoutExtension(defaultOutputFile.Name);
			return fs.FileInfo.New(Path.Combine(directory, $"{baseName}.md"));
		}
	}

	private string ApplySubstitutions(string content, MarkdownExportFileContext context)
	{
		// Get combined substitutions (global + file-specific)
		var substitutions = GetCombinedSubstitutions(context);

		// Process substitutions in the content
		foreach (var (key, value) in substitutions)
		{
			// Replace {{key}} with value
			content = content.Replace($"{{{{{key}}}}}", value);
		}

		return content;
	}

	private ConcurrentDictionary<string, string> GetCombinedSubstitutions(MarkdownExportFileContext context)
	{
		// Get global substitutions from BuildContext
		var globalSubstitutions = context.BuildContext.Configuration.Substitutions;

		// Get file-specific substitutions from YamlFrontMatter
		var fileSubstitutions = context.SourceFile.YamlFrontMatter?.Properties;

		// Create a new dictionary with all substitutions
		var allSubstitutions = new ConcurrentDictionary<string, string>();

		// Add file-specific substitutions first
		if (fileSubstitutions != null)
		{
			foreach (var (key, value) in fileSubstitutions)
			{
				_ = allSubstitutions.TryAdd(key, value);
			}
		}

		// Add global substitutions (will override file-specific ones if there are conflicts)
		foreach (var (key, value) in globalSubstitutions)
		{
			_ = allSubstitutions.TryAdd(key, value);
		}

		return allSubstitutions;
	}

	private string CreateLlmContentWithMetadata(MarkdownExportFileContext context, string llmMarkdown)
	{
		var sourceFile = context.SourceFile;
		var metadata = new StringBuilder();

		// Add metadata header
		// _ = metadata.AppendLine("<!-- LLM-Optimized Markdown Document -->");
		_ = metadata.AppendLine("---");
		// _ = metadata.AppendLine($"<!-- Source: {Path.GetRelativePath(context.BuildContext.DocumentationOutputDirectory.FullName, sourceFile.SourceFile.FullName)} -->");
		// _ = metadata.AppendLine($"<!-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC -->");
		_ = metadata.AppendLine($"title: {sourceFile.Title}");

		if (!string.IsNullOrEmpty(sourceFile.Url))
		{
			_ = metadata.AppendLine($"url: {context.BuildContext.CanonicalBaseUrl?.Scheme}://{context.BuildContext.CanonicalBaseUrl?.Host}{sourceFile.Url}");
		}

		if (!string.IsNullOrEmpty(sourceFile.YamlFrontMatter?.Description))
		{
			_ = metadata.AppendLine($"description: {sourceFile.YamlFrontMatter.Description}");
		}
		else
		{
			var descriptionGenerator = new DescriptionGenerator();
			var generateDescription = descriptionGenerator.GenerateDescription(context.Document);
			_ = metadata.AppendLine($"description: {generateDescription}");
		}


		var configProducts = context.BuildContext.Configuration.Products.Select(p =>
		{
			if (Products.AllById.TryGetValue(p, out var product))
				return product;
			throw new ArgumentException($"Invalid product id: {p}");
		});

		var frontMatterProducts = sourceFile.YamlFrontMatter?.Products ?? [];

		var allProducts = frontMatterProducts
			.Union(configProducts)
			.Distinct()
			.ToList();

		if (allProducts.Count > 0)
		{
			_ = metadata.AppendLine("products:");
			foreach (var product in allProducts.Select(p => p.DisplayName).Order())
				_ = metadata.AppendLine($"  - {product}");
		}

		_ = metadata.AppendLine("---");

		// Add an empty line after metadata
		_ = metadata.AppendLine();

		// Add the title as H1 heading
		_ = metadata.AppendLine($"# {sourceFile.Title}");

		// Add the converted markdown content
		_ = metadata.Append(llmMarkdown);

		return metadata.ToString();
	}
}

/// <summary>
/// Extension methods for easy integration with existing build configuration
/// </summary>
public static class LlmMarkdownExporterExtensions
{
	/// <summary>
	/// Adds LLM markdown export to the documentation generator with consistent rendering settings
	/// </summary>
	public static void AddLlmMarkdownExport(this List<IMarkdownExporter> exporters) => exporters.Add(new LlmMarkdownExporter());
}
