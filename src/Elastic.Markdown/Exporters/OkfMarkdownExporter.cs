// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Text;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Configuration.Products;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Components;
using Markdig.Syntax;

namespace Elastic.Markdown.Exporters;

/// <summary>
/// Exports markdown files as an <see href="https://github.com/GoogleCloudPlatform/knowledge-catalog/blob/main/okf/SPEC.md">
/// Open Knowledge Format (OKF) v0.1</see> conformant zip bundle ("okf.zip"). Unlike <see cref="LlmMarkdownExporter"/>,
/// links are rewritten to bundle-relative paths (not public URLs), and directory-listing <c>index.md</c> files are
/// synthesized rather than treated as content — authored landing pages are emitted as a sibling <c>{folder}.md</c>
/// concept next to the <c>{folder}/</c> directory (see <see cref="ComputeBundlePath"/>).
/// </summary>
public class OkfMarkdownExporter : IMarkdownExporter
{
	private const string ReservedIndexFileName = "index.md";

	private readonly ConcurrentBag<ConceptEntry> _entries = [];
	private IDirectoryInfo? _stagingDirectory;
	private readonly Lock _stagingLock = new();

	public ValueTask StartAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public ValueTask StopAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public async ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		if (IsUtilityPage(fileContext.SourceFile.YamlFrontMatter?.Layout))
			return true;

		// Remove the first H1 since we already emit the title as frontmatter + a synthesized heading —
		// don't rely on the HTML exporter (which isn't guaranteed to run) having already stripped it via
		// its own mutation of the shared Document instance. Mirrors ElasticsearchMarkdownExporter.ExportAsync.
		var h1 = fileContext.Document.Descendants<HeadingBlock>().FirstOrDefault(h => h.Level == 1);
		if (h1 is not null)
			_ = fileContext.Document.Remove(h1);

		var fs = fileContext.BuildContext.WriteFileSystem;
		var staging = GetOrCreateStagingDirectory(fs);

		var sourceFile = fileContext.SourceFile;
		var bundlePath = ComputeBundlePath(fileContext.NavigationItem.Url, fileContext.BuildContext.UrlPathPrefix);
		var content = CreateConceptContent(fileContext);

		var outputFile = fs.FileInfo.New(fs.Path.Combine(staging.FullName, bundlePath));
		if (outputFile.Directory is { Exists: false })
			outputFile.Directory.Create();

		await fs.File.WriteAllTextAsync(outputFile.FullName, content, Encoding.UTF8, ctx);

		_entries.Add(new ConceptEntry(bundlePath, sourceFile.Title, GetDescription(fileContext)));
		return true;
	}

	public async ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx)
	{
		var fs = outputFolder.FileSystem;
		var staging = GetOrCreateStagingDirectory(fs);

		await WriteIndexFilesAsync(fs, staging, ctx);

		// Nothing else guarantees outputFolder exists — unlike LlmMarkdownExporter, ExportAsync writes
		// into a temp staging dir rather than outputFolder, so this may be the first write to it.
		if (!outputFolder.Exists)
			outputFolder.Create();

		var zipPath = fs.Path.Combine(outputFolder.FullName, "okf.zip");
		await using var zipStream = fs.File.Create(zipPath);
		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);
		foreach (var file in staging.EnumerateFiles("*", SearchOption.AllDirectories))
		{
			var relativePath = fs.Path.GetRelativePath(staging.FullName, file.FullName).Replace('\\', '/');
			var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
			await using var entryStream = entry.Open();
			await using var sourceStream = fs.File.OpenRead(file.FullName);
			await sourceStream.CopyToAsync(entryStream, ctx);
		}

		staging.Delete(recursive: true);
		_stagingDirectory = null;
		return true;
	}

	private IDirectoryInfo GetOrCreateStagingDirectory(IFileSystem fs)
	{
		if (_stagingDirectory is { } existing)
			return existing;
		lock (_stagingLock)
		{
			if (_stagingDirectory is { } created)
				return created;
			var path = fs.Path.Combine(fs.Path.GetTempPath(), $"okf-export-{Guid.NewGuid():N}");
			var directory = fs.DirectoryInfo.New(path);
			directory.Create();
			_stagingDirectory = directory;
			return directory;
		}
	}

	/// <summary>
	/// Utility/system pages (404, search, archive) back site chrome, not documentation content — OKF has no
	/// use for them as concepts. Deliberately excludes <see cref="MarkdownPageLayout.LandingPage"/>: those are
	/// real authored landing pages, handled uniformly by <see cref="ComputeBundlePath"/>'s sibling-file mapping.
	/// </summary>
	internal static bool IsUtilityPage(MarkdownPageLayout? layout) =>
		layout is MarkdownPageLayout.NotFound or MarkdownPageLayout.Archive or MarkdownPageLayout.FullSearch;

	/// <summary>
	/// Maps a page's final site URL to its path within the OKF bundle — a pure function of the URL, needing
	/// no knowledge of other pages or which repo backs it. A folder's landing page URL has no trailing
	/// <c>/index</c> segment, so it naturally maps to a <em>sibling</em> <c>{folder}.md</c> file next to the
	/// <c>{folder}/</c> directory (mirrors <see cref="LlmMarkdownExporter"/>'s <c>GetLlmOutputFile</c> convention)
	/// rather than colliding with OKF's reserved, synthesized <c>index.md</c> directory listing.
	/// </summary>
	internal static string ComputeBundlePath(string navigationUrl, string? urlPathPrefix)
	{
		var prefix = urlPathPrefix ?? string.Empty;
		var url = navigationUrl;
		if (prefix.Length > 0 && url.StartsWith(prefix, StringComparison.Ordinal))
			url = url[prefix.Length..];
		var trimmed = url.Trim('/');
		return trimmed.Length == 0 ? "overview.md" : $"{trimmed}.md";
	}

	/// <summary>
	/// Derives the OKF <c>type</c> from the page's top-level navigation section (the first URL path
	/// segment after any configured <see cref="Elastic.Documentation.Configuration.BuildContext.UrlPathPrefix"/>),
	/// e.g. <c>/reference/query-languages/eql</c> -> <c>reference</c>.
	/// </summary>
	internal static string DeriveType(string navigationUrl, string? urlPathPrefix)
	{
		var prefix = urlPathPrefix ?? string.Empty;
		var url = navigationUrl;
		if (prefix.Length > 0 && url.StartsWith(prefix, StringComparison.Ordinal))
			url = url[prefix.Length..];
		var segments = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return segments.Length > 0 ? segments[0] : "documentation";
	}

	/// <summary>
	/// Rewrites an already-resolved link URL to its bundle-relative form via <see cref="ComputeBundlePath"/>.
	/// Internal links are normally host-relative paths, but some render paths (image/figure URLs, cross-links
	/// resolved via the assembler's production <see cref="Elastic.Documentation.Configuration.BuildContext.CanonicalBaseUrl"/>)
	/// absolutize even same-site links before this rewriter sees them — <paramref name="canonicalBaseUrl"/>
	/// lets us recognize and unwrap those self-referencing URLs back to bundle-relative form. Only URLs whose
	/// host genuinely differs (real external references) are left untouched — OKF tolerates those.
	/// </summary>
	internal static string? RewriteLinkUrl(string? url, string? urlPathPrefix, Uri? canonicalBaseUrl)
	{
		if (string.IsNullOrEmpty(url))
			return url;

		if (canonicalBaseUrl is not null
			&& Uri.TryCreate(url, UriKind.Absolute, out var absolute)
			&& string.Equals(absolute.Scheme, canonicalBaseUrl.Scheme, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(absolute.Host, canonicalBaseUrl.Host, StringComparison.OrdinalIgnoreCase))
			url = absolute.PathAndQuery + absolute.Fragment;

		if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
			return url;

		var anchorIndex = url.IndexOf('#');
		var path = anchorIndex >= 0 ? url[..anchorIndex] : url;
		var anchor = anchorIndex >= 0 ? url[anchorIndex..] : string.Empty;

		// TODO(api-explorer): /api/* pages are genuine third-party (OpenAPI-generated) reference endpoints —
		// they have no backing markdown file in this export, so link to the live site instead of a bundle path
		// that doesn't exist. Once ApiExplorer output is resolvable as markdown, remove this exception.
		if (IsApiReferencePath(path, urlPathPrefix))
			return canonicalBaseUrl is not null ? $"{new Uri(canonicalBaseUrl, path)}{anchor}" : $"{path}{anchor}";

		return $"/{ComputeBundlePath(path, urlPathPrefix)}{anchor}";
	}

	/// <summary>
	/// True when the (prefix-stripped) path falls under the <c>/api</c> section — see the TODO in
	/// <see cref="RewriteLinkUrl"/> for why these are excepted from bundle-relative rewriting.
	/// </summary>
	internal static bool IsApiReferencePath(string path, string? urlPathPrefix)
	{
		var prefix = urlPathPrefix ?? string.Empty;
		var stripped = prefix.Length > 0 && path.StartsWith(prefix, StringComparison.Ordinal) ? path[prefix.Length..] : path;
		stripped = stripped.Trim('/');
		return stripped == "api" || stripped.StartsWith("api/", StringComparison.Ordinal);
	}

	private static string CreateConceptContent(MarkdownExportFileContext context)
	{
		var urlPathPrefix = context.BuildContext.UrlPathPrefix;
		var canonicalBaseUrl = context.BuildContext.CanonicalBaseUrl;
		var body = DocumentationObjectPoolProvider.UseLlmMarkdownRenderer(context.BuildContext, url => RewriteLinkUrl(url, urlPathPrefix, canonicalBaseUrl), context.Document, static (renderer, document) =>
		{
			_ = renderer.Render(document);
		});

		var sourceFile = context.SourceFile;
		var frontMatter = DocumentationObjectPoolProvider.StringBuilderPool.Get();
		try
		{
			_ = frontMatter.AppendLine("---");
			_ = frontMatter.AppendLine($"type: {DeriveType(context.NavigationItem.Url, context.BuildContext.UrlPathPrefix)}");
			_ = frontMatter.AppendLine($"title: {sourceFile.Title}");
			if (!string.IsNullOrEmpty(sourceFile.YamlFrontMatter?.NavigationTitle))
				_ = frontMatter.AppendLine($"navigation_title: {sourceFile.YamlFrontMatter.NavigationTitle}");
			_ = frontMatter.AppendLine($"description: {GetDescription(context)}");
			_ = frontMatter.AppendLine($"resource: {GetResourceUrl(context)}");

			var tags = GetTags(context);
			if (tags.Count > 0)
			{
				_ = frontMatter.AppendLine("tags:");
				foreach (var tag in tags)
					_ = frontMatter.AppendLine($"  - {tag}");
			}
			_ = frontMatter.AppendLine("---");
			_ = frontMatter.AppendLine();
			_ = frontMatter.AppendLine($"# {sourceFile.Title}");
			_ = frontMatter.Append(body);
			// AppendLine uses Environment.NewLine — normalize so bundle content is identical regardless of the OS the build runs on.
			return frontMatter.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
		}
		finally
		{
			DocumentationObjectPoolProvider.StringBuilderPool.Return(frontMatter);
		}
	}

	private static string GetDescription(MarkdownExportFileContext context) =>
		!string.IsNullOrEmpty(context.SourceFile.YamlFrontMatter?.Description)
			? context.SourceFile.YamlFrontMatter.Description
			: new DescriptionGenerator().GenerateDescription(context.Document);

	private static string GetResourceUrl(MarkdownExportFileContext context) =>
		context.BuildContext.CanonicalBaseUrl is { } baseUrl
			? new Uri(baseUrl, context.NavigationItem.Url).ToString().TrimEnd('/')
			: context.NavigationItem.Url.TrimEnd('/');

	private static List<string> GetTags(MarkdownExportFileContext context)
	{
		var sourceFile = context.SourceFile;
		var inferrer = context.InferenceService ?? new NoopDocumentInferrer();
		var inference = inferrer.InferForMarkdown(
			context.BuildContext.Git.RepositoryName,
			sourceFile.YamlFrontMatter?.MappedPages,
			context.DocumentationSet.Configuration.Products,
			sourceFile.YamlFrontMatter?.Products,
			sourceFile.YamlFrontMatter?.AppliesTo
		);

		var tags = inference.RelatedProducts.Select(p => p.DisplayName).Order().ToList();

		var appliesTo = sourceFile.YamlFrontMatter?.AppliesTo;
		if (appliesTo is not null && appliesTo != ApplicableTo.All && appliesTo != ApplicableTo.Default)
			tags.AddRange(GetAppliesToItems(appliesTo, context.BuildContext));

		return tags;
	}

	private static List<string> GetAppliesToItems(ApplicableTo appliesTo, Elastic.Documentation.Configuration.BuildContext buildContext)
	{
		var viewModel = new ApplicableToViewModel
		{
			AppliesTo = appliesTo,
			Inline = true,
			ShowTooltip = true,
			VersionsConfig = buildContext.VersionsConfiguration
		};

		var items = viewModel.GetApplicabilityItems();
		return items.Select(item =>
		{
			var displayName = item.ApplicabilityDefinition.DisplayName.Replace("&nbsp;", " ");
			var popoverData = item.RenderData.PopoverData;
			var availabilityText = popoverData?.AvailabilityItems is { Length: > 0 }
				? string.Join(", ", popoverData.AvailabilityItems.Select(a => a.Text))
				: "Available";
			return $"{displayName}: {availabilityText}";
		}).ToList();
	}

	/// <summary>
	/// Synthesizes a reserved <c>index.md</c> per bundle directory (including the root), grouping concepts
	/// and subdirectories into markdown lists as described by OKF §6. Reserved index files carry no
	/// frontmatter, except the bundle-root index which declares <c>okf_version</c>.
	/// </summary>
	private async Task WriteIndexFilesAsync(IFileSystem fs, IDirectoryInfo staging, Cancel ctx)
	{
		var byDirectory = _entries
			.GroupBy(e => GetDirectory(e.BundlePath), StringComparer.Ordinal)
			.ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

		var directories = new HashSet<string>(byDirectory.Keys, StringComparer.Ordinal) { "" };
		foreach (var directory in byDirectory.Keys)
		{
			var parent = GetDirectory(directory);
			while (true)
			{
				_ = directories.Add(parent);
				if (parent.Length == 0)
					break;
				parent = GetDirectory(parent);
			}
		}

		var subdirectoriesByParent = directories
			.Where(d => d.Length > 0)
			.GroupBy(GetDirectory, StringComparer.Ordinal)
			.ToDictionary(g => g.Key, g => g.OrderBy(d => d, StringComparer.Ordinal).ToList(), StringComparer.Ordinal);

		foreach (var directory in directories)
		{
			var concepts = byDirectory.GetValueOrDefault(directory, []);
			var subdirectories = subdirectoriesByParent.GetValueOrDefault(directory, []);
			var content = RenderIndexContent(directory, concepts, subdirectories);

			var indexPath = directory.Length == 0
				? fs.Path.Combine(staging.FullName, ReservedIndexFileName)
				: fs.Path.Combine(staging.FullName, directory, ReservedIndexFileName);
			if (fs.Path.GetDirectoryName(indexPath) is { } dir && !fs.Directory.Exists(dir))
				_ = fs.Directory.CreateDirectory(dir);
			await fs.File.WriteAllTextAsync(indexPath, content, Encoding.UTF8, ctx);
		}
	}

	internal static string GetDirectory(string bundlePath)
	{
		var lastSlash = bundlePath.LastIndexOf('/');
		return lastSlash < 0 ? string.Empty : bundlePath[..lastSlash];
	}

	internal static string RenderIndexContent(
		string directory,
		IReadOnlyCollection<ConceptEntry> concepts,
		IReadOnlyCollection<string> subdirectories)
	{
		var sb = DocumentationObjectPoolProvider.StringBuilderPool.Get();
		try
		{
			if (directory.Length == 0)
				_ = sb.AppendLine("---").AppendLine("okf_version: \"0.1\"").AppendLine("---").AppendLine();

			if (concepts.Count > 0)
			{
				_ = sb.AppendLine("# Documents").AppendLine();
				foreach (var concept in concepts.OrderBy(c => c.BundlePath, StringComparer.Ordinal))
				{
					var fileName = concept.BundlePath[(concept.BundlePath.LastIndexOf('/') + 1)..];
					_ = sb.AppendLine($"* [{concept.Title}]({fileName}) - {concept.Description}");
				}
				_ = sb.AppendLine();
			}

			if (subdirectories.Count > 0)
			{
				_ = sb.AppendLine("# Subdirectories").AppendLine();
				foreach (var subdirectory in subdirectories)
				{
					var folderName = subdirectory[(subdirectory.LastIndexOf('/') + 1)..];
					// The subdirectory's landing page (if any) is a sibling file in THIS directory, not a child.
					var landingEntry = concepts.FirstOrDefault(e => e.BundlePath == $"{subdirectory}.md");
					var description = landingEntry?.Description;
					_ = string.IsNullOrEmpty(description)
						? sb.AppendLine($"* [{folderName}]({folderName}/)")
						: sb.AppendLine($"* [{folderName}]({folderName}/) - {description}");
				}
				_ = sb.AppendLine();
			}

			// AppendLine uses Environment.NewLine — normalize so bundle content is identical regardless of the OS the build runs on.
			return sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd() + "\n";
		}
		finally
		{
			DocumentationObjectPoolProvider.StringBuilderPool.Return(sb);
		}
	}

	internal sealed record ConceptEntry(string BundlePath, string Title, string Description);
}
