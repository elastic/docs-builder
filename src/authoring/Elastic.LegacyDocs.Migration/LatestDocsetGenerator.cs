// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.LegacyDocs.Migration.Asciidoc;
using Microsoft.Extensions.Logging;
using Slugify;

namespace Elastic.LegacyDocs.Migration;

public record LatestGeneratorOptions
{
	public required string OutputDirectory { get; init; }
	public string? BookFilter { get; init; }
	public required SourceRepoManager RepoManager { get; init; }
}

public class LatestDocsetGenerator(ILogger<LatestDocsetGenerator> logger)
{
	private static readonly SlugHelper SlugHelper = new();

	public async Task GenerateAsync(LegacyConf conf, LatestGeneratorOptions options, CancellationToken ct = default)
	{
		var books = conf.Contents
			.SelectMany(c => c.Sections)
			.Where(b => options.BookFilter is null || b.Prefix == options.BookFilter)
			.ToList();

		logger.LogInformation("Processing {BookCount} books in latest mode", books.Count);

		foreach (var book in books)
		{
			ct.ThrowIfCancellationRequested();

			if (string.IsNullOrEmpty(book.Current))
			{
				logger.LogWarning("No current version for {BookPrefix} — skipping", book.Prefix);
				continue;
			}

			await ProcessBook(book, options, ct);
		}

		logger.LogInformation("Latest generation complete");
	}

	private async Task ProcessBook(LegacyBook book, LatestGeneratorOptions options, CancellationToken ct)
	{
		var version = book.Current;
		var sources = options.RepoManager.ResolveSources(book, version);
		if (sources.Count == 0)
		{
			logger.LogWarning("No sources resolved for {Prefix}", book.Prefix);
			return;
		}

		var primarySource = sources[0];
		if (primarySource.LocalPath is null)
		{
			logger.LogWarning("No local path for {Repo} — skipping {Prefix}", primarySource.RepoName, book.Prefix);
			return;
		}

		var indexPath = Path.Combine(primarySource.LocalPath, book.Index);
		if (!File.Exists(indexPath))
		{
			logger.LogWarning("Index file not found: {IndexPath}", indexPath);
			return;
		}

		var content = await File.ReadAllTextAsync(indexPath, ct);
		var basePath = Path.GetDirectoryName(indexPath) ?? primarySource.LocalPath;
		var parserOptions = new AsciidocParserOptions
		{
			Attributes = new Dictionary<string, string>
			{
				["branch"] = version,
				["doc-tests-src"] = primarySource.LocalPath
			}
		};
		var parser = new AsciidocParser(parserOptions);
		var document = parser.Parse(content, basePath);

		var emitterOptions = new MarkdownEmitterOptions
		{
			BookPrefix = book.Prefix,
			Version = version
		};
		var emitter = new MarkdownEmitter(emitterOptions);
		var pages = PageChunker.Chunk(document, book.Chunk, emitter);

		if (pages.Count == 0)
		{
			logger.LogWarning("No pages generated for {Prefix}", book.Prefix);
			return;
		}

		var repoName = primarySource.RepoName;
		var docsDir = Path.Combine(options.OutputDirectory, repoName, "docs");
		_ = Directory.CreateDirectory(docsDir);

		var fileEntries = new List<TocEntry>();
		foreach (var page in pages)
		{
			var filename = $"{page.Slug}.md";
			await File.WriteAllTextAsync(Path.Combine(docsDir, filename), page.MarkdownContent, ct);
			fileEntries.Add(new TocEntry { File = filename });
		}

		var projectName = SlugHelper.GenerateSlug(book.Title);
		YamlWriter.WriteDocsetYaml(Path.Combine(docsDir, "docset.yml"), projectName, ["."]);
		YamlWriter.WriteTocYaml(Path.Combine(docsDir, "toc.yml"), fileEntries);

		logger.LogInformation("Wrote {PageCount} pages for {RepoName}/docs (project: {Project})", pages.Count, repoName, projectName);
	}
}
