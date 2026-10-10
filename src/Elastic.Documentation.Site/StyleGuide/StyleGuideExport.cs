// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Compression;
using Microsoft.Extensions.FileProviders;

namespace Elastic.Documentation.Site.StyleGuide;

/// <summary>Packs the style guide as a folder of static HTML pages beside the design-system assets, so it can be shared or opened offline.</summary>
public static class StyleGuideExport
{
	private const string StaticFolder = "_static";

	// The legacy bundle is not part of the design system.
	private static bool IsLegacyBundle(string name) =>
		name.StartsWith("styles.", StringComparison.Ordinal)
			|| name.StartsWith("main.", StringComparison.Ordinal)
			|| name.EndsWith(".map", StringComparison.Ordinal);

	/// <param name="staticFiles">The design-system assets.</param>
	/// <param name="previewRenderer">Renders a whole-page preview by id, or returns <c>null</c> when it cannot. Previews are left out when this is not given.</param>
	public static async Task<byte[]> BuildZipAsync(
		IFileProvider staticFiles,
		Func<string, Task<string?>>? previewRenderer,
		Cancel ctx = default
	)
	{
		using var stream = new MemoryStream();
		using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
		{
			foreach (var page in StyleGuideCatalog.Pages)
			{
				var html = await StyleGuideCatalog.RenderPageAsync(page.Slug, path => $"{StaticFolder}/{path}", offline: true, ctx);
				if (html is null)
					continue;
				var relinked = RelinkPages(html);
				AddText(zip, $"{page.Slug}.html", relinked);
				if (page == StyleGuideCatalog.Default)
					AddText(zip, "index.html", relinked);
			}

			if (previewRenderer is not null)
			{
				foreach (var entry in StyleGuidePreviewCatalog.Entries)
				{
					if (await previewRenderer(entry.Id) is { } preview)
						AddText(
							zip,
							$"preview/{entry.Id}.html",
							preview.Replace("/_static/", $"../{StaticFolder}/", StringComparison.Ordinal)
						);
				}
			}

			foreach (var file in staticFiles.GetDirectoryContents(string.Empty).Where(f => !f.IsDirectory && !IsLegacyBundle(f.Name)))
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
