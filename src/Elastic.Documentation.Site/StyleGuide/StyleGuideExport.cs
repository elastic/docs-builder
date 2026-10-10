// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;

namespace Elastic.Documentation.Site.StyleGuide;

/// <summary>Packs the style guide as a folder of static HTML pages beside the design-system assets, so it can be shared or opened offline.</summary>
public static partial class StyleGuideExport
{
	private const string StaticFolder = "_static";

	private const string Readme =
		"""
		The Elastic docs design-system style guide, as static pages.

		Open index.html. Pages, colours, type and layout work from the folder as it is. The scripts (theme and motion toggles, menus,
		search) are ES modules, which browsers refuse to load from a file:// address, so for those serve the folder instead:

		    npx serve .        or        python3 -m http.server

		Preview pages (preview/) are whole pages of the isolated, assembler, Codex and API builds. Links that leave the style guide
		point at the live site and do not resolve here.
		""";

	// Files that are part of the design system's entry points; everything else is pulled in by being referenced.
	private static readonly string[] EntryPoints = ["ds.js", "ds.css", "ds-layers.css", "ds-guide.css"];

	// A file name as it appears in the text of a page, script or stylesheet.
	[GeneratedRegex(@"[A-Za-z0-9_.@-]+\.(?:js|css|woff2?|ttf|png|svg|jpe?g|webp|gif|avif)")]
	private static partial Regex AssetName();

	/// <summary>The files a set of pages needs: the entry points and whatever the pages, scripts and stylesheets refer to, followed to the end.</summary>
	internal static async Task<IReadOnlyCollection<IFileInfo>> ReachableAsync(
		IFileProvider staticFiles,
		IEnumerable<string> pages,
		Cancel ctx
	)
	{
		var available = staticFiles
			.GetDirectoryContents(string.Empty)
			.Where(f => !f.IsDirectory && !f.Name.EndsWith(".map", StringComparison.Ordinal))
			.ToDictionary(f => f.Name, StringComparer.Ordinal);
		var reached = new Dictionary<string, IFileInfo>(StringComparer.Ordinal);
		var queue = new Queue<string>(EntryPoints.Where(available.ContainsKey));
		foreach (var page in pages)
			Enqueue(AssetName().Matches(page).Select(m => m.Value));

		while (queue.Count > 0)
		{
			var name = queue.Dequeue();
			if (!available.TryGetValue(name, out var file) || !reached.TryAdd(name, file))
				continue;
			if (!name.EndsWith(".js", StringComparison.Ordinal) && !name.EndsWith(".css", StringComparison.Ordinal))
				continue;
			await using var source = file.CreateReadStream();
			using var reader = new StreamReader(source);
			Enqueue(AssetName().Matches(await reader.ReadToEndAsync(ctx)).Select(m => Path.GetFileName(m.Value)));
		}
		return reached.Values;

		void Enqueue(IEnumerable<string> names)
		{
			foreach (var n in names)
				queue.Enqueue(n);
		}
	}

	/// <param name="staticFiles">The design-system assets.</param>
	/// <param name="previewRenderer">Renders a whole-page preview by id, or returns <c>null</c> when it cannot. Previews are left out when this is not given.</param>
	public static async Task<byte[]> BuildZipAsync(
		IFileProvider staticFiles,
		Func<string, Task<string?>>? previewRenderer,
		Cancel ctx = default
	)
	{
		using var stream = new MemoryStream();
		var pages = new List<(string Name, string Html)>();
		foreach (var page in StyleGuideCatalog.Pages)
		{
			var html = await StyleGuideCatalog.RenderPageAsync(page.Slug, path => $"{StaticFolder}/{path}", offline: true, ctx);
			if (html is null)
				continue;
			var relinked = RelinkPages(html);
			pages.Add(($"{page.Slug}.html", relinked));
			if (page == StyleGuideCatalog.Default)
				pages.Add(("index.html", relinked));
		}

		if (previewRenderer is not null)
		{
			foreach (var entry in StyleGuidePreviewCatalog.Entries)
			{
				if (await previewRenderer(entry.Id) is { } preview)
					pages.Add(($"preview/{entry.Id}.html", preview.Replace("/_static/", $"../{StaticFolder}/", StringComparison.Ordinal)));
			}
		}

		using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
		{
			foreach (var (name, html) in pages)
				AddText(zip, name, html);
			AddText(zip, "README.txt", Readme);

			foreach (var file in await ReachableAsync(staticFiles, pages.Select(p => p.Html), ctx))
			{
				var entry = zip.CreateEntry($"{StaticFolder}/{file.Name}", CompressionLevel.Optimal);
				await using var target = entry.Open();
				await using var source = file.CreateReadStream();
				await source.CopyToAsync(target, ctx);
			}
		}
		return stream.ToArray();
	}

	/// <summary>Points links between guide pages at the exported files instead of the dev-server routes.</summary>
	public static string RelinkPages(string html)
	{
		foreach (var entry in StyleGuidePreviewCatalog.Entries)
			html = html.Replace(
				$"\"{StyleGuidePreviewCatalog.Route}/{entry.Id}\"",
				$"\"preview/{entry.Id}.html\"",
				StringComparison.Ordinal
			);
		foreach (var page in StyleGuideCatalog.Pages)
			html = html.Replace($"href=\"{StyleGuideCatalog.Route}/{page.Slug}\"", $"href=\"{page.Slug}.html\"", StringComparison.Ordinal);
		return html.Replace($"href=\"{StyleGuideCatalog.Route}\"", "href=\"index.html\"", StringComparison.Ordinal);
	}

	private static void AddText(ZipArchive zip, string name, string content)
	{
		var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
		using var writer = new StreamWriter(entry.Open());
		writer.Write(content);
	}
}
