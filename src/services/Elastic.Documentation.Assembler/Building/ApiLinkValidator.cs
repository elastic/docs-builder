// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Assembler.Building;

public sealed record ApiLinkReportEntry(string Url, string Source, int Line);

public sealed record ApiLinkReport(int Checked, IReadOnlyList<ApiLinkReportEntry> Broken);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiLinkReport))]
internal sealed partial class ApiLinkReportJsonContext : JsonSerializerContext;

/// <summary>Checks links recorded during the Markdown build against the API pages the assembler generated.</summary>
public static class ApiLinkValidator
{
	public const string ReportFileName = "api-link-report.json";

	public static IReadOnlyList<ApiLinkReference> FindBroken(IEnumerable<ApiLinkReference> links, IReadOnlySet<string> generatedUrls) =>
		[
			.. links
				.Where(l => ApiLinkMatcher.TryGetApiPath(l.Url, out var path) && !generatedUrls.Contains(path))
				.OrderBy(l => l.Url, StringComparer.Ordinal)
				.ThenBy(l => l.File, StringComparer.Ordinal)
				.ThenBy(l => l.Line)
		];

	/// <summary>Emits a warning per broken link and writes the report; returns the report path, or null when nothing was checked.</summary>
	public static async Task<string?> ValidateAsync(
		IDiagnosticsCollector collector,
		IReadOnlySet<string> generatedUrls,
		IFileSystem fileSystem,
		string outputDirectory,
		Cancel ctx
	)
	{
		// Without generated pages every API link would look broken, so skip instead of flooding the build.
		if (generatedUrls.Count == 0)
			return null;

		// A snippet is parsed once per page that includes it, and a card's targets are seen by more than one parser,
		// so report each URL once per file, at its first position.
		var links = collector
			.ApiLinks
			.OrderBy(l => l.File, StringComparer.Ordinal)
			.ThenBy(l => l.Line)
			.ThenBy(l => l.Column)
			.DistinctBy(l => (l.Url, l.File))
			.ToArray();
		if (links.Length == 0)
			return null;

		var broken = FindBroken(links, generatedUrls);
		foreach (var link in broken)
		{
			collector.Write(new Diagnostic
			{
				Severity = Severity.Warning,
				File = link.File,
				Line = link.Line,
				Column = link.Column,
				Length = link.Length,
				Message = $"API link '{link.Url}' does not resolve to a generated API page. Point it at an existing page " +
					"(for older endpoints use a versioned tree such as /docs/api/doc/<product>/v8/...) or confirm the endpoint was removed intentionally."
			});
		}

		var report = new ApiLinkReport(links.Length, [.. broken.Select(b => new ApiLinkReportEntry(b.Url, b.File, b.Line))]);
		var path = fileSystem.Path.Join(outputDirectory, ReportFileName);
		var json = JsonSerializer.Serialize(report, ApiLinkReportJsonContext.Default.ApiLinkReport);
		await fileSystem
			.File
			.WriteAllTextAsync(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), ctx)
			.ConfigureAwait(false);
		return path;
	}
}
