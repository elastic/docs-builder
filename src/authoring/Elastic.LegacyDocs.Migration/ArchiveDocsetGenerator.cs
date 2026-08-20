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
	public int? MinMajorVersion { get; init; }
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

			var versions = GetVersionsToProcess(book, options.AllVersions, options.MinMajorVersion);
			if (versions.Count == 0)
			{
				logger.LogWarning("No versions to process for {BookPrefix}", book.Prefix);
				continue;
			}

			logger.LogInformation("Book {Prefix}: {Count} versions to process", book.Prefix, versions.Count);

			var prefixDir = Path.Combine(options.OutputDirectory, book.Prefix);
			var versionEntries = new List<TocEntry>();

			foreach (var version in versions)
			{
				ct.ThrowIfCancellationRequested();

				var versionLabel = version.VersionLabel;
				try
				{
					var pages = await ProcessBookVersion(book, version, options, ct);
					if (pages.Count == 0)
						continue;

					var versionDir = Path.Combine(prefixDir, versionLabel);
					_ = Directory.CreateDirectory(versionDir);

					var fileEntries = await WritePages(pages, versionDir, ct);
					YamlWriter.WriteTocYaml(Path.Combine(versionDir, "toc.yml"), fileEntries);
					versionEntries.Add(new TocEntry { Folder = versionLabel });

					logger.LogInformation("Wrote {PageCount} pages for {Prefix}/{Version}", pages.Count, book.Prefix, versionLabel);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					logger.LogError(ex, "Failed to process {Prefix}/{Version}", book.Prefix, versionLabel);
				}
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
		LegacyBook book,
		BranchRef version,
		ArchiveGeneratorOptions options,
		CancellationToken ct
	)
	{
		var versionLabel = version.VersionLabel;
		var sources = await options.RepoManager.ResolveSourcesAsync(book, version, ct);
		if (sources.Count == 0)
		{
			logger.LogWarning("No sources resolved for {Prefix} version {Version}", book.Prefix, versionLabel);
			return [];
		}

		var primarySource = sources[0];
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
			Attributes = new Dictionary<string, string> { ["branch"] = versionLabel, ["doc-tests-src"] = primarySource.LocalPath }
		};
		var parser = new AsciidocParser(parserOptions);
		var document = parser.Parse(content, basePath);

		var emitterOptions = new MarkdownEmitterOptions { BookPrefix = book.Prefix, Version = versionLabel };
		var emitter = new MarkdownEmitter(emitterOptions);

		return PageChunker.Chunk(document, book.Chunk, emitter);
	}

	private static async Task<List<TocEntry>> WritePages(IReadOnlyList<PageOutput> pages, string directory, CancellationToken ct)
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

	internal static List<BranchRef> GetVersionsToProcess(LegacyBook book, bool allVersions, int? minMajorVersion = null)
	{
		var branches = FilterByMinVersion(book.Branches, minMajorVersion);

		if (allVersions)
			return SortBranchesDescending(branches);

		var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (!string.IsNullOrEmpty(book.Current))
			_ = selected.Add(book.Current);

		var grouped = branches.Select(b => (Branch: b, Parsed: TryParseMajorMinor(b.VersionLabel)))
			.Where(x => x.Parsed.HasValue)
			.GroupBy(x => x.Parsed!.Value.Major);

		foreach (var group in grouped)
		{
			var topTwo = group.OrderByDescending(x => x.Parsed!.Value.Minor).Take(2);

			foreach (var (branch, _) in topTwo)
				_ = selected.Add(branch.VersionLabel);
		}

		return SortBranchesDescending(branches.Where(b => selected.Contains(b.VersionLabel)));
	}

	private static List<BranchRef> FilterByMinVersion(IEnumerable<BranchRef> branches, int? minMajor)
	{
		if (minMajor is null)
			return branches.ToList();

		return branches.Where(b =>
		{
			var parsed = TryParseMajorMinor(b.VersionLabel);
			return parsed.HasValue && parsed.Value.Major >= minMajor;
		}).ToList();
	}

	private static List<BranchRef> SortBranchesDescending(IEnumerable<BranchRef> branches) =>
		branches.Select(b => (Branch: b, Parsed: TryParseMajorMinor(b.VersionLabel)))
			.OrderByDescending(x => x.Parsed?.Major ?? 0)
			.ThenByDescending(x => x.Parsed?.Minor ?? 0)
			.Select(x => x.Branch)
			.ToList();

	private static (int Major, int Minor)? TryParseMajorMinor(string version)
	{
		var parts = version.Split('.');
		if (parts.Length >= 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
			return (major, minor);

		return null;
	}
}
