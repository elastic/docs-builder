// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Compression;
using System.Text;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Markdown.Helpers;
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
			var llmsTxt = Path.Combine(outputDirectory, "llms.txt");
			var llmsTxtRelativePath = Path.GetRelativePath(outputDirectory, llmsTxt);
			_ = zip.CreateEntryFromFile(llmsTxt, llmsTxtRelativePath);

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
		var llmMarkdown = ConvertToLlmMarkdown(fileContext.Document, fileContext.BuildContext);
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

	public static string ConvertToLlmMarkdown(MarkdownDocument document, BuildContext context) =>
		DocumentationObjectPoolProvider.UseLlmMarkdownRenderer(context, document, static (renderer, obj) =>
		{
			_ = renderer.Render(obj);
		});

	private static IFileInfo GetLlmOutputFile(MarkdownExportFileContext fileContext)
	{
		var source = fileContext.SourceFile.SourceFile;
		var fs = source.FileSystem;
		var defaultOutputFile = fileContext.DefaultOutputFile;

		var fileName = Path.GetFileNameWithoutExtension(defaultOutputFile.Name);
		if (fileName == "index")
		{
			var root = fileContext.BuildContext.OutputDirectory;

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
		var metadata = DocumentationObjectPoolProvider.StringBuilderPool.Get();

		_ = metadata.AppendLine("---");
		_ = metadata.AppendLine($"title: {sourceFile.Title}");

		if (!string.IsNullOrEmpty(sourceFile.YamlFrontMatter?.Description))
			_ = metadata.AppendLine($"description: {sourceFile.YamlFrontMatter.Description}");
		else
		{
			var descriptionGenerator = new DescriptionGenerator();
			var generateDescription = descriptionGenerator.GenerateDescription(context.Document);
			_ = metadata.AppendLine($"description: {generateDescription}");
		}

		if (!string.IsNullOrEmpty(sourceFile.Url))
			_ = metadata.AppendLine($"url: {context.BuildContext.CanonicalBaseUrl?.Scheme}://{context.BuildContext.CanonicalBaseUrl?.Host}{sourceFile.Url}");

		var pageProducts = GetPageProducts(sourceFile.YamlFrontMatter?.Products);
		if (pageProducts.Count > 0)
		{
			_ = metadata.AppendLine("products:");
			foreach (var item in pageProducts.Select(p => p.DisplayName).Order())
				_ = metadata.AppendLine($"  - {item}");
		}

		_ = metadata.AppendLine("---");
		_ = metadata.AppendLine();
		_ = metadata.AppendLine($"# {sourceFile.Title}");
		_ = metadata.Append(llmMarkdown);

		return metadata.ToString();
	}

	private static List<Product> GetPageProducts(IReadOnlyCollection<Product>? frontMatterProducts) =>
		frontMatterProducts?.ToList() ?? [];
}

public static class LlmMarkdownExporterExtensions
{
	public static void AddLlmMarkdownExport(this List<IMarkdownExporter> exporters) => exporters.Add(new LlmMarkdownExporter());
}
