// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>
/// Golden snapshots of what today's loaders produce for the frozen inputs in <c>Baseline/</c>.
/// Each snapshot is a semantic projection (value + diagnostics), written by hand so it does not depend on
/// the shape of the types that produced it. A refactored loader must reproduce the same snapshot.
/// Regenerate with <c>GOLDEN_UPDATE=1</c> only when a behavior change is intended and reviewed.
/// </summary>
public static class Golden
{
	private static readonly JsonSerializerOptions WriteOptions = new()
	{
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	public static string BaselineDirectory => Path.Join(AppContext.BaseDirectory, "Baseline");

	public static string BaselineFile(params string[] parts) => Path.Join([BaselineDirectory, .. parts]);

	/// <summary>A provider reading the frozen <c>Baseline/config/</c> files, through a mock file system seeded as the Remote config source.</summary>
	public static ConfigurationFileProvider BaselineProvider(bool skipPrivateRepositories = false)
	{
		var fs = new MockFileSystem();
		var configDirectory = ConfigurationFileProvider.AppDataConfigurationDirectory;
		foreach (var file in Directory.EnumerateFiles(BaselineFile("config")))
			fs.AddFile(Path.Join(configDirectory, Path.GetFileName(file)), new MockFileData(File.ReadAllText(file)));
		fs.AddFile(Path.Join(configDirectory, "git-ref.txt"), new MockFileData("golden"));
		return new ConfigurationFileProvider(
			NullLoggerFactory.Instance,
			new ConfigurationFileSystem(fs),
			skipPrivateRepositories,
			ConfigurationSource.Remote
		);
	}

	/// <summary>Captures a load as either its projected value or the exception it throws today.</summary>
	public static JsonNode Capture(Func<JsonNode?> load)
	{
		try
		{
			return Obj(("value", load()));
		}
		catch (Exception e)
		{
			return Obj(("failure", Failure(e)));
		}
	}

	private static string SourceGoldenDirectory([CallerFilePath] string callerFile = "") =>
		Path.Join(Path.GetDirectoryName(callerFile)!, "Snapshots");

	/// <summary>Compares <paramref name="actual"/> against the committed snapshot, or rewrites it when GOLDEN_UPDATE=1.</summary>
	public static async Task AssertMatches(string name, JsonNode actual)
	{
		var json = Serialize(actual);
		var path = Path.Join(SourceGoldenDirectory(), $"{name}.json");
		if (Environment.GetEnvironmentVariable("GOLDEN_UPDATE") == "1")
		{
			_ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			await File.WriteAllTextAsync(path, json);
			return;
		}

		File.Exists(path).Should().BeTrue($"golden snapshot {name}.json must exist; run with GOLDEN_UPDATE=1 to create it");
		var expected = await File.ReadAllTextAsync(path);
		json
			.ReplaceLineEndings()
			.Should()
			.Be(expected.ReplaceLineEndings(), $"the loader output for {name} must match the golden snapshot");
	}

	public static string Serialize(JsonNode node) => node.ToJsonString(WriteOptions) + Environment.NewLine;

	/// <summary>Builds an object whose keys are written in the given order.</summary>
	public static JsonObject Obj(params (string Key, JsonNode? Value)[] members)
	{
		var o = new JsonObject();
		foreach (var (key, value) in members)
			o[key] = value;
		return o;
	}

	/// <summary>Builds an object from a map, with keys sorted ordinally so snapshots are stable.</summary>
	public static JsonObject Map<TKey, TValue>(
		IEnumerable<KeyValuePair<TKey, TValue>> map,
		Func<TValue, JsonNode?> project
	) where TKey : notnull
	{
		var o = new JsonObject();
		foreach (var (key, value) in map.OrderBy(kv => kv.Key.ToString(), StringComparer.Ordinal))
			o[key.ToString()!] = project(value);
		return o;
	}

	/// <summary>Builds an array, keeping source order (order is meaningful in most configuration lists).</summary>
	public static JsonArray Arr<T>(IEnumerable<T> items, Func<T, JsonNode?> project)
	{
		var a = new JsonArray();
		foreach (var item in items)
			a.Add(project(item));
		return a;
	}

	/// <summary>Builds a sorted array of strings, for sets where order carries no meaning.</summary>
	public static JsonArray Set(IEnumerable<string> items) => Arr(items.Order(StringComparer.Ordinal), s => JsonValue.Create(s));

	public static JsonValue? Str(string? value) => value is null ? null : JsonValue.Create(value);

	public static JsonValue Bool(bool value) => JsonValue.Create(value);

	public static JsonValue Num(int value) => JsonValue.Create(value);

	/// <summary>Projects diagnostics with absolute paths rewritten relative to known roots.</summary>
	public static JsonArray DiagnosticList(IEnumerable<Diagnostic> diagnostics) =>
		Arr(
			diagnostics,
			d => Obj(
				("severity", Str(d.Severity.ToString())),
				("file", Str(NormalizePath(d.File))),
				("line", d.Line is { } l ? Num(l) : null),
				("column", d.Column is { } c ? Num(c) : null),
				("message", Str(NormalizePath(d.Message)))
			)
		);

	/// <summary>
	/// Captures a failed load by its message. The exception type is an implementation detail: a refactored loader
	/// may report the same failure as an error diagnostic, but it must keep the message.
	/// </summary>
	public static JsonObject Failure(Exception e) => Failure(e.Message);

	public static JsonObject Failure(string message) => Obj(("message", Str(NormalizePath(message))));

	/// <summary>Rewrites machine-specific absolute paths to stable placeholders.</summary>
	public static string NormalizePath(string value)
	{
		if (string.IsNullOrEmpty(value))
			return value;
		var result = value.Replace(BaselineDirectory, "<baseline>", StringComparison.Ordinal);
		result = result.Replace(Paths.ApplicationData.FullName, "<appdata>", StringComparison.Ordinal);
		if (Paths.GetSolutionDirectory() is { } solution)
			result = result.Replace(solution.FullName, "<solution>", StringComparison.Ordinal);
		return result.Replace('\\', '/');
	}
}

/// <summary>Collects diagnostics in memory, in emission order.</summary>
public sealed class CapturingDiagnosticsCollector() : DiagnosticsCollector([])
{
	private readonly List<Diagnostic> _diagnostics = [];

	public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

	public override void Write(Diagnostic diagnostic)
	{
		IncrementSeverityCount(diagnostic);
		_diagnostics.Add(diagnostic);
	}

	public override DiagnosticsCollector StartAsync(Cancel ctx) => this;

	public override Task StopAsync(Cancel cancellationToken) => Task.CompletedTask;
}
