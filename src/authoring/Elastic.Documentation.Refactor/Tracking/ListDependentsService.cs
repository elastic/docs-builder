// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Services;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Refactor.Tracking;

public class ListDependentsService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ListDependentsService>();

	public async Task<bool> ListDependents(
		IDiagnosticsCollector collector,
		ScopedFileSystem fs,
		string? path,
		IReadOnlyList<string> files,
		string format,
		Cancel ctx)
	{
		if (files.Count == 0)
		{
			collector.EmitGlobalError("list-dependents requires at least one file argument.");
			return false;
		}

		if (!format.Equals("json", StringComparison.OrdinalIgnoreCase) &&
			!format.Equals("text", StringComparison.OrdinalIgnoreCase))
		{
			collector.EmitGlobalError($"Unsupported format '{format}'. Expected 'json' or 'text'.");
			return false;
		}

		var context = new BuildContext(collector, fs, fs, configurationContext, ExportOptions.MetadataOnly, path, null);
		var set = new DocumentationSet(context, logFactory, NoopCrossLinkResolver.Instance);

		var graph = await IncludeGraph.BuildAsync(set, ctx);

		var sourceDir = context.DocumentationSourceDirectory;
		var gitRoot = Paths.FindGitRoot(sourceDir);

		var results = files
			.Select(input => ResolveOne(input, sourceDir.FullName, gitRoot?.FullName, graph))
			.ToArray();

		var output = format.Equals("text", StringComparison.OrdinalIgnoreCase)
			? RenderText(results)
			: RenderJson(results);
		Console.Out.WriteLine(output);

		var totalDependents = results.Sum(r => r.Dependents.Count);
		_logger.LogInformation(
			"Resolved {Inputs} input(s) → {Pages} page dependents", results.Length, totalDependents);
		return true;
	}

	private static DependentsResult ResolveOne(string input, string sourceDirFullName, string? gitRootFullName, IncludeGraph graph)
	{
		var absolute = Path.IsPathRooted(input)
			? input
			: gitRootFullName is not null
				? Path.GetFullPath(Path.Combine(gitRootFullName, input))
				: Path.GetFullPath(input);

		var sourceRelative = Path.GetRelativePath(sourceDirFullName, absolute);
		if (sourceRelative.StartsWith("..", StringComparison.Ordinal))
		{
			return new DependentsResult(input, sourceRelative.OptionalWindowsReplace(), false, "outside documentation source", []);
		}

		var normalized = sourceRelative.Replace('\\', '/');
		if (!graph.HasConsumers(normalized))
			return new DependentsResult(input, normalized, false, "no consumers found", []);

		var dependents = graph.ResolvePageDependents(normalized).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
		return new DependentsResult(input, normalized, true, null, dependents);
	}

	private static string RenderJson(IReadOnlyList<DependentsResult> results)
	{
		var payload = new DependentsPayload(results);
		return JsonSerializer.Serialize(payload, ListDependentsJsonContext.Default.DependentsPayload);
	}

	private static string RenderText(IReadOnlyList<DependentsResult> results)
	{
		var sb = new System.Text.StringBuilder();
		foreach (var r in results)
		{
			if (!r.Found)
			{
				_ = sb.Append(r.Input).Append(" → no dependents (").Append(r.Reason ?? "unknown").AppendLine(")");
				continue;
			}
			_ = sb.Append(r.Input).Append(" → ").Append(r.Dependents.Count).AppendLine(" dependent page(s):");
			foreach (var dep in r.Dependents)
				_ = sb.Append("  ").AppendLine(dep);
		}
		return sb.ToString().TrimEnd();
	}
}

public sealed record DependentsPayload(
	[property: JsonPropertyName("results")] IReadOnlyList<DependentsResult> Results);

public sealed record DependentsResult(
	[property: JsonPropertyName("input")] string Input,
	[property: JsonPropertyName("resolved")] string Resolved,
	[property: JsonPropertyName("found")] bool Found,
	[property: JsonPropertyName("reason")] string? Reason,
	[property: JsonPropertyName("dependents")] IReadOnlyList<string> Dependents);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DependentsPayload))]
internal sealed partial class ListDependentsJsonContext : JsonSerializerContext;
