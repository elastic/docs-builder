// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cysharp.IO;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Serialization;

namespace Elastic.Markdown.Extensions.DetectionRules;

/// <summary>
/// Represents version information from version.lock.json
/// </summary>
public record VersionLockEntry
{
	[JsonPropertyName("rule_name")]
	public string? RuleName { get; init; }

	[JsonPropertyName("sha256")]
	public string? Sha256 { get; init; }

	[JsonPropertyName("type")]
	public string? Type { get; init; }

	[JsonPropertyName("version")]
	public int Version { get; init; }
}

[JsonSerializable(typeof(Dictionary<string, VersionLockEntry>))]
internal sealed partial class VersionLockJsonContext : JsonSerializerContext;

[TomlSerializable(typeof(TomlTable))]
internal sealed partial class DetectionRuleTomlContext : TomlSerializerContext;

public record DetectionRuleThreat
{
	public required string Framework { get; init; }
	public required DetectionRuleTechnique[] Techniques { get; init; } = [];
	public required DetectionRuleTactic Tactic { get; init; }
}

public record DetectionRuleTactic
{
	public required string Id { get; init; }
	public required string Name { get; init; }
	public required string Reference { get; init; }
}

public record DetectionRuleSubTechnique
{
	public required string Id { get; init; }
	public required string Name { get; init; }
	public required string Reference { get; init; }
}

public record DetectionRuleTechnique : DetectionRuleSubTechnique
{
	public required DetectionRuleSubTechnique[] SubTechniques { get; init; } = [];
}

public record DetectionRule
{
	// Cached version lock data, loaded once per build
	private static FrozenDictionary<string, VersionLockEntry>? VersionLock;

	public required string Name { get; init; }

	public required string[]? Authors { get; init; }

	public required string? Note { get; init; }

	public required string? Query { get; init; }

	public required string? Setup { get; init; }

	public required string[]? Tags { get; init; }

	public string? Domain => Tags?.FirstOrDefault(t => t.StartsWith("Domain:", StringComparison.Ordinal))?[7..]?.Trim();

	public required string Severity { get; init; }

	public required string RuleId { get; init; }

	public required int RiskScore { get; init; }

	public required string License { get; init; }

	public required string Description { get; init; }
	public required string Type { get; init; }
	public required string? Language { get; init; }
	public required string[]? Indices { get; init; }
	public required string? RunsEvery { get; init; }
	public required string? IndicesFromDateMath { get; init; }
	public required int MaximumAlertsPerExecution { get; init; }
	public required string[]? References { get; init; }
	public required int Version { get; init; }

	public required DetectionRuleThreat[] Threats { get; init; } = [];

	public required string? DeprecationDate { get; init; }
	public required string? Maturity { get; init; }

	/// <summary>
	/// Initializes the version lock cache from the version.lock.json file.
	/// This should be called once before processing detection rules.
	/// </summary>
	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	public static void InitializeVersionLock(IFileSystem fileSystem, IDirectoryInfo? checkoutDirectory)
	{
		if (VersionLock != null || checkoutDirectory == null)
			return;

		var versionLockPath = fileSystem.Path.Join(
			checkoutDirectory.FullName,
			"detection_rules",
			"etc",
			"version.lock.json"
		);

		if (!fileSystem.File.Exists(versionLockPath))
			return;

		try
		{
			var json = fileSystem.File.ReadAllText(versionLockPath, Encoding.UTF8);
			var versionData = JsonSerializer.Deserialize(json, VersionLockJsonContext.Default.DictionaryStringVersionLockEntry);
			VersionLock = versionData?.ToFrozenDictionary() ?? FrozenDictionary<string, VersionLockEntry>.Empty;
		}
		catch
		{
			// If we can't load the version lock, continue without it
			VersionLock = FrozenDictionary<string, VersionLockEntry>.Empty;
		}
	}

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	public static DetectionRule From(IFileInfo source)
	{
		TomlTable model;
		try
		{
			using var reader = new Utf8StreamReader(source.FullName, fileOpenMode: FileOpenMode.Throughput);
			var sourceText = Encoding.UTF8.GetString(reader.ReadToEndAsync().GetAwaiter().GetResult());
			model = TomlSerializer.Deserialize(sourceText, DetectionRuleTomlContext.Default.TomlTable)!;
		}
		catch (Exception e)
		{
			throw new Exception($"Could not parse toml in: {source.FullName}", e);
		}

		if (!model.TryGetValue("metadata", out var metadataObj) || metadataObj is not TomlTable metadata)
			throw new Exception($"Could not find metadata section in {source.FullName}");

		if (!model.TryGetValue("rule", out var ruleObj) || ruleObj is not TomlTable rule)
			throw new Exception($"Could not find rule section in {source.FullName}");

		try
		{
			return BuildRule(metadata, rule);
		}
		catch (Exception e)
		{
			throw new Exception($"Could not read fields from: {source.FullName}", e);
		}
	}

	internal static DetectionRule FromToml(string toml)
	{
		var model = TomlSerializer.Deserialize(toml, DetectionRuleTomlContext.Default.TomlTable)!;
		var metadata = (TomlTable)model["metadata"]!;
		var rule = (TomlTable)model["rule"]!;
		return BuildRule(metadata, rule);
	}

	private static DetectionRule BuildRule(TomlTable metadata, TomlTable rule)
	{
		var threats = GetThreats(rule);
		var ruleId = GetString(rule, "rule_id");

		// Get max_signals from TOML, default to 100 if not specified
		var maxSignals = TryGetInt(rule, "max_signals") ?? 100;

		// Look up version from version.lock.json, default to 1 if not found
		var version = 1;
		if (VersionLock != null && VersionLock.TryGetValue(ruleId, out var versionEntry))
			version = versionEntry.Version;

		return new DetectionRule
		{
			Authors = TryGetStringArray(rule, "author"),
			Description = GetString(rule, "description"),
			Type = GetString(rule, "type"),
			Language = TryGetString(rule, "language"),
			License = GetString(rule, "license"),
			RiskScore = TryGetInt(rule, "risk_score") ?? 0,
			RuleId = ruleId,
			Severity = GetString(rule, "severity"),
			Tags = TryGetStringArray(rule, "tags"),
			Indices = TryGetStringArray(rule, "index"),
			References = TryGetStringArray(rule, "references"),
			IndicesFromDateMath = TryGetString(rule, "from"),
			Setup = TryGetString(rule, "setup"),
			Query = TryGetString(rule, "query"),
			Note = TryGetString(rule, "note"),
			Name = GetString(rule, "name"),
			RunsEvery = TryGetString(rule, "interval"),
			MaximumAlertsPerExecution = maxSignals,
			Version = version,
			Threats = threats,
			DeprecationDate = TryGetString(metadata, "deprecation_date"),
			Maturity = TryGetString(metadata, "maturity")
		};
	}

	private static DetectionRuleThreat[] GetThreats(TomlTable model)
	{
		if (!model.TryGetValue("threat", out var node) || node is not TomlTableArray threats)
			return [];

		var threatsList = new List<DetectionRuleThreat>(threats.Count);
		foreach (var threatTable in threats.OfType<TomlTable>())
		{
			var framework = GetString(threatTable, "framework");
			var techniques = ReadTechniques(threatTable);
			var tactic = ReadTactic(threatTable);
			threatsList.Add(new DetectionRuleThreat
			{
				Framework = framework,
				Techniques = techniques,
				Tactic = tactic
			});
		}

		return [.. threatsList];
	}

	private static DetectionRuleTechnique[] ReadTechniques(TomlTable threatTable)
	{
		if (!threatTable.TryGetValue("technique", out var node) || node is not TomlTableArray techniquesArray)
			return [];

		var techniques = new List<DetectionRuleTechnique>(techniquesArray.Count);
		foreach (var techniqueTable in techniquesArray.OfType<TomlTable>())
		{
			techniques.Add(new DetectionRuleTechnique
			{
				Id = GetString(techniqueTable, "id"),
				Name = GetString(techniqueTable, "name"),
				Reference = GetString(techniqueTable, "reference"),
				SubTechniques = ReadSubTechniques(techniqueTable)
			});
		}
		return [.. techniques];
	}

	private static DetectionRuleSubTechnique[] ReadSubTechniques(TomlTable techniqueTable)
	{
		if (!techniqueTable.TryGetValue("subtechnique", out var node) || node is not TomlTableArray subArray)
			return [];

		var subTechniques = new List<DetectionRuleSubTechnique>(subArray.Count);
		foreach (var subTable in subArray.OfType<TomlTable>())
		{
			subTechniques.Add(new DetectionRuleSubTechnique
			{
				Id = GetString(subTable, "id"),
				Name = GetString(subTable, "name"),
				Reference = GetString(subTable, "reference")
			});
		}
		return [.. subTechniques];
	}

	private static DetectionRuleTactic ReadTactic(TomlTable threatTable)
	{
		var tacticTable = (TomlTable)threatTable["tactic"];
		return new DetectionRuleTactic
		{
			Id = GetString(tacticTable, "id"),
			Name = GetString(tacticTable, "name"),
			Reference = GetString(tacticTable, "reference")
		};
	}

	private static string GetString(TomlTable table, string key) =>
		(string)table[key];

	private static string[]? TryGetStringArray(TomlTable table, string key) =>
		table.TryGetValue(key, out var node) && node is TomlArray t ? t.OfType<string>().ToArray() : null;

	private static string? TryGetString(TomlTable table, string key) =>
		table.TryGetValue(key, out var node) && node is string s ? s : null;

	private static int? TryGetInt(TomlTable table, string key) =>
		table.TryGetValue(key, out var node) && node is long l ? (int)l : null;
}
