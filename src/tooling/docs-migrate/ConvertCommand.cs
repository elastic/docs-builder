// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using ConsoleAppFramework;
using Elastic.LegacyDocs.Migration;
using Elastic.LegacyDocs.Migration.Asciidoc;
using Microsoft.Extensions.Logging;

namespace Documentation.Migrate;

internal sealed class ConvertCommand(ILoggerFactory logFactory)
{
	private readonly ILogger _logger = logFactory.CreateLogger<ConvertCommand>();

	/// <summary>Convert AsciiDoc books to Markdown docsets.</summary>
	/// <param name="workDir">Working directory for migration artifacts</param>
	/// <param name="majors">Override: number of top major versions to include</param>
	/// <param name="all">Override: process all versions</param>
	/// <param name="minVersion">Override: minimum major version to process</param>
	/// <param name="book">Override: filter to books whose prefix starts with this value</param>
	/// <param name="ctx">Cancellation token</param>
	[Command("")]
	public async Task<int> Convert(
		string? workDir = null,
		int? majors = null,
		bool? all = null,
		int? minVersion = null,
		string? book = null,
		Cancel ctx = default
	)
	{
		var dir = SharedOptions.ResolveWorkDir(workDir);
		var conf = await SharedOptions.LoadConfAsync(dir, ctx);

		var opts = SharedOptions.ResolveFilterOptions(dir, majors, all, minVersion, book);
		_logger.LogInformation(
			"Filter: majors={Majors}, all={All}, minVersion={MinVersion}, book={Book}",
			opts.Majors, opts.All, opts.MinVersion ?? (object)"any", opts.Book ?? "all");

		var books = SharedOptions.FilterBooks(conf, opts.Book);

		var reposDir = Path.Combine(dir, "repos");
		var outputDir = Path.Combine(dir, "output");

		var sparsePaths = SourceRepoManager.CollectSparsePaths(conf);
		var repoOptions = new SourceRepoOptions { ReposDirectory = reposDir, RepoUrls = conf.Repos, SparsePaths = sparsePaths };
		var repoManager = new SourceRepoManager(repoOptions, logFactory.CreateLogger<SourceRepoManager>());

		var convertedBooks = new Dictionary<string, List<string>>();
		var tocRefs = new List<string>();

		foreach (var b in books)
		{
			ctx.ThrowIfCancellationRequested();

			var versions = SharedOptions.FilterVersions(b, opts.Majors, opts.All, opts.MinVersion);
			if (versions.Count == 0)
			{
				_logger.LogWarning("No versions to process for {BookPrefix}", b.Prefix);
				continue;
			}

			_logger.LogInformation("Book {Prefix}: {Count} versions to process", b.Prefix, versions.Count);

			var prefixDir = Path.Combine(outputDir, b.Prefix);
			var versionEntries = new List<TocEntry>();
			var convertedVersions = new List<string>();

			foreach (var version in versions)
			{
				ctx.ThrowIfCancellationRequested();

				var versionLabel = version.VersionLabel;
				try
				{
					var pages = await ProcessBookVersion(b, version, repoManager, ctx);
					if (pages.Count == 0)
						continue;

					var versionDir = Path.Combine(prefixDir, versionLabel);
					_ = Directory.CreateDirectory(versionDir);

					var fileEntries = await WritePages(pages, versionDir, ctx);
					YamlWriter.WriteTocYaml(Path.Combine(versionDir, "toc.yml"), fileEntries);
					versionEntries.Add(new TocEntry { Folder = versionLabel });
					convertedVersions.Add(versionLabel);

					_logger.LogInformation("Wrote {PageCount} pages for {Prefix}/{Version}",
						pages.Count, b.Prefix, versionLabel);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					_logger.LogError(ex, "Failed to process {Prefix}/{Version}", b.Prefix, versionLabel);
				}
			}

			if (versionEntries.Count == 0)
				continue;

			tocRefs.Add(b.Prefix);
			convertedBooks[b.Prefix] = convertedVersions;

			YamlWriter.WriteTocYaml(Path.Combine(prefixDir, "toc.yml"), [
				new TocEntry { File = "index.md" },
				..versionEntries
			]);

			WriteBookVersionIndex(prefixDir, b, convertedVersions);
		}

		if (tocRefs.Count > 0)
		{
			WriteGuideOverview(outputDir, conf, convertedBooks);
			WriteRootDocsetYaml(outputDir, tocRefs);
		}

		_logger.LogInformation("Conversion complete: {BookCount} books written to {OutputDir}", tocRefs.Count, outputDir);
		return 0;
	}

	private async Task<IReadOnlyList<PageOutput>> ProcessBookVersion(
		LegacyBook book, BranchRef version, SourceRepoManager repoManager, CancellationToken ct)
	{
		var versionLabel = version.VersionLabel;
		var sources = await repoManager.ResolveSourcesAsync(book, version, ct);
		if (sources.Count == 0)
		{
			_logger.LogWarning("No sources resolved for {Prefix} version {Version}", book.Prefix, versionLabel);
			return [];
		}

		var primarySource = sources[0];
		var indexPath = Path.Combine(primarySource.LocalPath, book.Index);
		if (!File.Exists(indexPath))
		{
			_logger.LogWarning("Index file not found: {IndexPath}", indexPath);
			return [];
		}

		var content = await File.ReadAllTextAsync(indexPath, ct);
		var basePath = Path.GetDirectoryName(indexPath) ?? primarySource.LocalPath;
		var parserOptions = new AsciidocParserOptions
		{
			Attributes = new Dictionary<string, string>
			{
				["branch"] = versionLabel,
				["doc-tests-src"] = primarySource.LocalPath
			}
		};
		var parser = new AsciidocParser(parserOptions);
		var document = parser.Parse(content, basePath);

		var emitterOptions = new MarkdownEmitterOptions
		{
			BookPrefix = book.Prefix,
			Version = versionLabel
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

	private static void WriteBookVersionIndex(string prefixDir, LegacyBook book, List<string> versions)
	{
		var sb = new StringBuilder();
		_ = sb.Append("# ").Append(book.Title).AppendLine(" — All Versions");
		_ = sb.AppendLine();
		_ = sb.AppendLine("| Version | Status |");
		_ = sb.AppendLine("|---|---|");

		foreach (var v in versions)
		{
			var status = v == book.Current ? "current" : "";
			_ = sb.Append("| [").Append(v).Append("](").Append(v).Append("/index.md) | ").Append(status).AppendLine(" |");
		}

		File.WriteAllText(Path.Combine(prefixDir, "index.md"), sb.ToString());
	}

	private static void WriteGuideOverview(
		string outputDir, LegacyConf conf, Dictionary<string, List<string>> convertedBooks)
	{
		var sb = new StringBuilder();
		_ = sb.AppendLine("# Elastic Docs");
		_ = sb.AppendLine();

		foreach (var category in conf.Contents)
		{
			var categoryBooks = category.Sections
				.Where(b => convertedBooks.ContainsKey(b.Prefix))
				.ToList();

			if (categoryBooks.Count == 0)
				continue;

			_ = sb.Append("## ").AppendLine(category.Title);

			foreach (var b in categoryBooks)
			{
				var current = !string.IsNullOrEmpty(b.Current) ? b.Current : convertedBooks[b.Prefix][0];
				_ = sb.Append("- [").Append(b.Title).Append(" [").Append(current).Append("]](")
					.Append(b.Prefix).Append('/').Append(current).Append("/index.md) — [other versions](")
					.Append(b.Prefix).AppendLine("/index.md)");
			}

			_ = sb.AppendLine();
		}

		File.WriteAllText(Path.Combine(outputDir, "index.md"), sb.ToString());
	}

	private static void WriteRootDocsetYaml(string outputDir, List<string> tocRefs)
	{
		var sb = new StringBuilder();
		_ = sb.AppendLine("project: elastic-guide-archive");
		_ = sb.AppendLine("features:");
		_ = sb.AppendLine("  guide-nav: true");
		_ = sb.AppendLine("toc:");
		_ = sb.AppendLine("  - file: index.md");

		foreach (var prefix in tocRefs)
			_ = sb.Append("  - toc: ").AppendLine(prefix);

		File.WriteAllText(Path.Combine(outputDir, "docset.yml"), sb.ToString());
	}
}
