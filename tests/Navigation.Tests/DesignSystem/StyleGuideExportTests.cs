// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Compression;
using AwesomeAssertions;
using Elastic.Documentation.Site.StyleGuide;
using Microsoft.Extensions.FileProviders;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

public class StyleGuideExportTests
{
	private static async Task<ZipArchive> ExportAsync(Func<string, Task<string?>>? previewRenderer = null, bool hashedEntry = false)
	{
		var folder = Directory.CreateTempSubdirectory("style-guide-export");
		if (hashedEntry)
		{
			_ = Directory.CreateDirectory(Path.Join(folder.FullName, ".vite"));
			await File.WriteAllTextAsync(
				Path.Join(folder.FullName, ".vite", "manifest.json"),
				"{\"Assets/ds.ts\":{\"file\":\"ds-ABC123.js\",\"name\":\"ds\",\"isEntry\":true}}",
				TestContext.Current!.Execution.CancellationToken
			);
			await File.WriteAllTextAsync(
				Path.Join(folder.FullName, "ds-ABC123.js"),
				"export {}",
				TestContext.Current!.Execution.CancellationToken
			);
		}
		await File.WriteAllTextAsync(Path.Join(folder.FullName, "ds.css"), "body{}", TestContext.Current!.Execution.CancellationToken);
		await File.WriteAllTextAsync(
			Path.Join(folder.FullName, "styles.1234.css"),
			"legacy{}",
			TestContext.Current!.Execution.CancellationToken
		);
		using var provider = new PhysicalFileProvider(folder.FullName);
		var bytes = await StyleGuideExport.BuildZipAsync(provider, previewRenderer, TestContext.Current!.Execution.CancellationToken);
		return new ZipArchive(new MemoryStream(bytes));
	}

	private static string Read(ZipArchive zip, string name)
	{
		using var reader = new StreamReader(zip.GetEntry(name)!.Open());
		return reader.ReadToEnd();
	}

	[Test]
	public async Task Zip_HoldsEveryPageAsStaticHtmlNextToTheAssets()
	{
		using var zip = await ExportAsync();
		var names = zip.Entries.Select(e => e.FullName).ToList();

		names.Should().Contain("index.html").And.Contain("decisions.html").And.Contain("_static/ds.css");
		names.Should().NotContain(n => n.Contains("styles.1234", StringComparison.Ordinal));
		names
			.Where(n => n.EndsWith(".html", StringComparison.Ordinal) && !n.Contains('/', StringComparison.Ordinal))
			.Should()
			.HaveCount(StyleGuideCatalog.Pages.Count + 1);
	}

	[Test]
	public async Task Pages_ReferenceTheHashedEntryBundleThatVitePacked()
	{
		using var zip = await ExportAsync(hashedEntry: true);
		var html = Read(zip, "decisions.html");

		zip.Entries.Select(e => e.FullName).Should().Contain("_static/ds-ABC123.js");
		html.Should().Contain("_static/ds-ABC123.js").And.NotContain("_static/ds.js\"");
	}

	[Test]
	public async Task Pages_UseRelativeAssetsAndLinkToEachOther()
	{
		using var zip = await ExportAsync();
		var html = Read(zip, "decisions.html");

		html.Should().Contain("href=\"_static/ds.css\"");
		html.Should().Contain("href=\"foundations.html\"");
		html.Should().NotContain("href=\"/style-guide");
		html.Should().NotContain("download.zip");
	}

	[Test]
	public async Task Zip_HoldsEveryPagePreviewWithItsAssetsRelinked()
	{
		using var zip = await ExportAsync(id => Task.FromResult<string?>($"<link href=\"/_static/ds.css\"><p>{id}</p>"));
		var names = zip.Entries.Select(e => e.FullName).ToList();

		foreach (var entry in StyleGuidePreviewCatalog.Entries)
			names.Should().Contain($"preview/{entry.Id}.html");
		Read(zip, $"preview/{StyleGuidePreviewCatalog.Entries[0].Id}.html").Should().Contain("href=\"../_static/ds.css\"");
	}

	[Test]
	public async Task Zip_HoldsTheLayoutSamplesTheBreakpointsPageFrames()
	{
		using var zip = await ExportAsync();
		var names = zip.Entries.Select(e => e.FullName).ToList();

		foreach (var name in LayoutSamples.Names)
			names.Should().Contain($"samples/{name}.html");
		Read(zip, "samples/three-full.html").Should().Contain("layout-full").And.Contain("href=\"../_static/ds.css\"");
		Read(zip, "breakpoints.html").Should().Contain("src=\"samples/three.html\"").And.NotContain("/style-guide/layout-sample");
	}
}
