// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Compression;
using System.Text;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Markdown.Helpers;
using Markdig.Syntax;

namespace Elastic.Markdown.Exporters;

/// <summary>
/// Exports markdown files as LLM-optimized CommonMark using custom renderers
/// </summary>
public class LlmMarkdownExporter : IMarkdownExporter
{
	private const string LlmsTxtTemplate = """
		# Elastic Documentation
		
		> Elastic provides an open source search, analytics, and AI platform, and out-of-the-box solutions for observability and security. The Search AI platform combines the power of search and generative AI to provide near real-time search and analysis with relevance to reduce your time to value.
		>
		>Elastic offers the following solutions or types of projects:
		>
		>* [Elasticsearch](/solutions/search.md): Build powerful search and RAG applications using Elasticsearch's vector database, AI toolkit, and advanced retrieval capabilities.  
		>* [Elastic Observability](/solutions/observability.md): Gain comprehensive visibility into applications, infrastructure, and user experience through logs, metrics, traces, and other telemetry data, all in a single interface.
		>* [Elastic Security](/solutions/security.md): Combine SIEM, endpoint security, and cloud security to provide comprehensive tools for threat detection and prevention, investigation, and response.
		
		The documentation is organized to guide you through your journey with Elastic, from learning the basics to deploying and managing complex solutions. Here is a detailed breakdown of the documentation structure:
		
		* [**Elastic fundamentals**](/get-started.md): Understand the basics about the deployment options, platform, and solutions, and features of the documentation.  
		* [**Solutions and use cases**](/solutions.md): Learn use cases, evaluate, and implement Elastic's solutions: Observability, Search, and Security.  
		* [**Manage data**](/manage-data.md): Learn about data store primitives, ingestion and enrichment, managing the data lifecycle, and migrating data.  
		* [**Explore and analyze**](/explore-analyze.md): Get value from data through querying, visualization, machine learning, and alerting.  
		* [**Deploy and manage**](/deploy-manage.md): Deploy and manage production-ready clusters. Covers deployment options and maintenance tasks.  
		* [**Manage your Cloud account**](/cloud-account.md): A dedicated section for user-facing cloud account tasks like resetting passwords.  
		* [**Troubleshoot**](/troubleshoot.md): Identify and resolve problems.  
		* [**Extend and contribute**](/extend.md): How to contribute to or integrate with Elastic, from open source to plugins to integrations.  
		* [**Release notes**](/release-notes.md): Contains release notes and changelogs for each new release.  
		* [**Reference**](/reference.md): Reference material for core tasks and manuals for optional products.
      
		""";

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

		string content;
		if (IsRootIndexFile(fileContext))
		{
			// Use template for root llms.txt file
			content = LlmsTxtTemplate;
		}
		else
		{
			// Regular markdown files get metadata + content
			content = CreateLlmContentWithMetadata(fileContext, llmMarkdown);
		}

		await fileContext.SourceFile.SourceFile.FileSystem.File.WriteAllTextAsync(
			outputFile.FullName,
			content,
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

	private static bool IsRootIndexFile(MarkdownExportFileContext fileContext)
	{
		var defaultOutputFile = fileContext.DefaultOutputFile;
		var fileName = Path.GetFileNameWithoutExtension(defaultOutputFile.Name);
		if (fileName != "index")
			return false;

		var root = fileContext.BuildContext.OutputDirectory;
		return defaultOutputFile.Directory!.FullName == root.FullName;
	}

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
