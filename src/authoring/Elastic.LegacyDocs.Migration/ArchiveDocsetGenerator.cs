// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.LegacyDocs.Migration.Asciidoc;
using Microsoft.Extensions.Logging;

namespace Elastic.LegacyDocs.Migration;

public record ArchiveGeneratorOptions
{
	public required string OutputDirectory { get; init; }
	public string? BookFilter { get; init; }
	public bool AllVersions { get; init; }
	public required SourceRepoManager RepoManager { get; init; }
}

public class ArchiveDocsetGenerator(ILogger<ArchiveDocsetGenerator> logger)
{
	public async Task GenerateAsync(LegacyConf conf, ArchiveGeneratorOptions options, CancellationToken ct = default)
	{
		var books = conf.Contents
			.SelectMany(c => c.Sections)
			.Where(b => options.BookFilter is null || b.Prefix == options.BookFilter)
			.ToList();

		logger.LogInformation("Processing {BookCount} books in archive mode", books.Count);

		var tocRefs = new List<string>();

		foreach (var book in books)
		{
			ct.ThrowIfCancellationRequested();

			var versions = GetVersionsToProcess(book, options.AllVersions);
			if (versions.Count == 0)
			{
				logger.LogWarning("No versions to process for {BookPrefix}", book.Prefix);
				continue;
			}

			var prefixDir = Path.Combine(options.OutputDirectory, book.Prefix);
			var versionEntries = new List<TocEntry>();

			foreach (var version in versions)
			{
				ct.ThrowIfCancellationRequested();

				var pages = await ProcessBookVersion(book, version, options, ct);
				if (pages.Count == 0)
					continue;

				var versionDir = Path.Combine(prefixDir, version);
				_ = Directory.CreateDirectory(versionDir);

				var fileEntries = await WritePages(pages, versionDir, ct);
				YamlWriter.WriteTocYaml(Path.Combine(versionDir, "toc.yml"), fileEntries);
				versionEntries.Add(new TocEntry { Folder = version });

				logger.LogInformation("Wrote {PageCount} pages for {Prefix}/{Version}", pages.Count, book.Prefix, version);
			}

			if (versionEntries.Count > 0)
			{
				tocRefs.Add(book.Prefix);
				YamlWriter.WriteTocYaml(Path.Combine(prefixDir, "toc.yml"), versionEntries);
			}
		}

		if (tocRefs.Count > 0)
			YamlWriter.WriteDocsetYaml(Path.Combine(options.OutputDirectory, "docset.yml"), "guide-archive", tocRefs);

		logger.LogInformation("Archive generation complete: {BookCount} books", tocRefs.Count);
	}

	private async Task<IReadOnlyList<PageOutput>> ProcessBookVersion(
		LegacyBook book, string version, ArchiveGeneratorOptions options, CancellationToken ct)
	{
		var sources = options.RepoManager.ResolveSources(book, version);
		if (sources.Count == 0)
		{
			logger.LogWarning("No sources resolved for {Prefix} version {Version}", book.Prefix, version);
			return [];
		}

		var primarySource = sources[0];
		if (primarySource.LocalPath is null)
		{
			logger.LogWarning("No local path for {Repo} — skipping {Prefix}/{Version}", primarySource.RepoName, book.Prefix, version);
			return [];
		}

		var indexPath = Path.Combine(primarySource.LocalPath, book.Index);
		if (!File.Exists(indexPath))
		{
			logger.LogWarning("Index file not found: {IndexPath}", indexPath);
			return [];
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

		return PageChunker.Chunk(document, book.Chunk, emitter);
	}

	private static async Task<List<TocEntry>> WritePages(
		IReadOnlyList<PageOutput> pages, string directory, CancellationToken ct)
	{
		var entries = new List<TocEntry>();
		foreach (var page in pages)
		{
			var filename = $"{page.Slug}.md";
			await File.WriteAllTextAsync(Path.Combine(directory, filename), page.MarkdownContent, ct);
			entries.Add(new TocEntry { File = filename });
		}
		return entries;
	}

	internal static List<string> GetVersionsToProcess(LegacyBook book, bool allVersions)
	{
		if (allVersions)
			return SortVersionsDescending(book.Live);

		var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (!string.IsNullOrEmpty(book.Current))
			_ = selected.Add(book.Current);

		var grouped = book.Live
			.Select(v => (Version: v, Parsed: TryParseMajorMinor(v)))
			.Where(x => x.Parsed.HasValue)
			.GroupBy(x => x.Parsed!.Value.Major);

		foreach (var group in grouped)
		{
			var topTwo = group
				.OrderByDescending(x => x.Parsed!.Value.Minor)
				.Take(2)
				.Select(x => x.Version);

			foreach (var v in topTwo)
				_ = selected.Add(v);
		}

		return SortVersionsDescending(selected);
	}

	private static List<string> SortVersionsDescending(IEnumerable<string> versions) =>
		versions
			.Select(v => (Version: v, Parsed: TryParseMajorMinor(v)))
			.OrderByDescending(x => x.Parsed?.Major ?? 0)
			.ThenByDescending(x => x.Parsed?.Minor ?? 0)
			.Select(x => x.Version)
			.ToList();

	private static (int Major, int Minor)? TryParseMajorMinor(string version)
	{
		var parts = version.Split('.');
		if (parts.Length >= 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
			return (major, minor);

		return null;
	}
}
