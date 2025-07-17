// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Text;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.Renderers;
using Elastic.Markdown.Myst.Renderers.LlmMarkdown;
using Markdig.Syntax;

namespace Elastic.Markdown.Exporters;

/// <summary>
/// Exports markdown files as LLM-optimized CommonMark using custom renderers
/// </summary>
public class LlmMarkdownExporter : IMarkdownExporter
{

	public ValueTask StartAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public ValueTask StopAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx)
	{
		var outputDirectory = Path.Combine(outputFolder.FullName, "docs");
		var zipPath = Path.Combine(outputDirectory, "llm.zip");
		using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
		{
			var llmsTxt = Path.Combine(outputFolder.FullName, "llms.txt");
			_ = zip.CreateEntryFromFile(llmsTxt, "llms.txt");

			var markdownFiles = Directory.GetFiles(outputDirectory, "*.md", SearchOption.AllDirectories);

			foreach (var file in markdownFiles)
			{
				var relativePath = Path.GetRelativePath(outputDirectory, file);
				_ = zip.CreateEntryFromFile(file, relativePath);
			}
		}
		return ValueTask.FromResult(true);
	}

	public async ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		var llmMarkdown = ConvertToLlmMarkdown(fileContext.Document, fileContext);
		var outputFile = GetLlmOutputFile(fileContext);
		if (outputFile.Directory is { Exists: false })
			outputFile.Directory.Create();
		var contentWithMetadata = CreateLlmContentWithMetadata(fileContext, llmMarkdown);
		await fileContext.SourceFile.SourceFile.FileSystem.File.WriteAllTextAsync(
			outputFile.FullName,
			contentWithMetadata,
			Encoding.UTF8,
			ctx
		);
		return true;
	}

	public static string ConvertToLlmMarkdown(MarkdownDocument document, MarkdownExportFileContext context)
	{
		using var writer = new StringWriter();
		var renderer = new LlmMarkdownRenderer(writer)
		{
			BuildContext = context.BuildContext
		};
		_ = renderer.Render(document);
		return writer.ToString();
	}

	private static IFileInfo GetLlmOutputFile(MarkdownExportFileContext fileContext)
	{
		var source = fileContext.SourceFile.SourceFile;
		var fs = source.FileSystem;
		var defaultOutputFile = fileContext.DefaultOutputFile;

		var fileName = Path.GetFileNameWithoutExtension(defaultOutputFile.Name);
		if (fileName == "index")
		{
			var root = fileContext.BuildContext.DocumentationOutputDirectory;

			if (defaultOutputFile.Directory!.FullName == root.FullName)
				return fs.FileInfo.New(Path.Combine(root.FullName, "llms.txt"));

			// For index files: /docs/section/index.html -> /docs/section.md
			// This allows users to append .md to any URL path
			var folderName = defaultOutputFile.Directory!.Name;
			return fs.FileInfo.New(Path.Combine(
				defaultOutputFile.Directory!.Parent!.FullName,
				$"{folderName}.md"
			));
		}
		// Regular files: /docs/section/page.html -> /docs/section/page.llm.md
		var directory = defaultOutputFile.Directory!.FullName;
		var baseName = Path.GetFileNameWithoutExtension(defaultOutputFile.Name);
		return fs.FileInfo.New(Path.Combine(directory, $"{baseName}.md"));
	}


	private string CreateLlmContentWithMetadata(MarkdownExportFileContext context, string llmMarkdown)
	{
		var sourceFile = context.SourceFile;
		var metadata = new StringBuilder();

		_ = metadata.AppendLine("---");
		_ = metadata.AppendLine($"title: {sourceFile.Title}");

		if (!string.IsNullOrEmpty(sourceFile.Url))
			_ = metadata.AppendLine($"url: {context.BuildContext.CanonicalBaseUrl?.Scheme}://{context.BuildContext.CanonicalBaseUrl?.Host}{sourceFile.Url}");

		if (!string.IsNullOrEmpty(sourceFile.YamlFrontMatter?.Description))
			_ = metadata.AppendLine($"description: {sourceFile.YamlFrontMatter.Description}");
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
		_ = metadata.AppendLine();
		_ = metadata.AppendLine($"# {sourceFile.Title}");
		_ = metadata.Append(llmMarkdown);

		return metadata.ToString();
	}
}

public static class LlmMarkdownExporterExtensions
{
	public static void AddLlmMarkdownExport(this List<IMarkdownExporter> exporters) => exporters.Add(new LlmMarkdownExporter());
}
