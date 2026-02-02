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
using Tomlet;
using Tomlet.Models;

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
	// Reuse a single TomlParser instance for better performance
	private static readonly TomlParser Parser = new();

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

	/// <summary>
	/// Initializes the version lock cache from the version.lock.json file.
	/// This should be called once before processing detection rules.
	/// </summary>
	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	public static void InitializeVersionLock(IFileSystem fileSystem, IDirectoryInfo? checkoutDirectory)
	{
		if (VersionLock != null || checkoutDirectory == null)
			return;

		var versionLockPath = fileSystem.Path.Combine(
			checkoutDirectory.FullName,
			"detection_rules",
			"etc",
			"version.lock.json"
		);

		if (!fileSystem.File.Exists(versionLockPath))
			return;

		try
		{
			using var reader = new Utf8StreamReader(versionLockPath, fileOpenMode: FileOpenMode.Throughput);
			var json = Encoding.UTF8.GetString(reader.ReadToEndAsync().GetAwaiter().GetResult());
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
		TomlDocument model;
		try
		{
			// Use optimized Utf8StreamReader for better I/O performance
			using var reader = new Utf8StreamReader(source.FullName, fileOpenMode: FileOpenMode.Throughput);
			var sourceText = Encoding.UTF8.GetString(reader.ReadToEndAsync().GetAwaiter().GetResult());
			model = Parser.Parse(sourceText);
		}
		catch (Exception e)
		{
			throw new Exception($"Could not parse toml in: {source.FullName}", e);
		}

		if (!model.TryGetValue("metadata", out var node) || node is not TomlTable)
			throw new Exception($"Could not find metadata section in {source.FullName}");

		if (!model.TryGetValue("rule", out node) || node is not TomlTable rule)
			throw new Exception($"Could not find rule section in {source.FullName}");

		var threats = GetThreats(rule);
		var ruleId = rule.GetString("rule_id");

		// Get max_signals from TOML, default to 100 if not specified
		var maxSignals = TryGetInt(rule, "max_signals") ?? 100;

		// Look up version from version.lock.json, default to 1 if not found
		var version = 1;
		if (VersionLock != null && VersionLock.TryGetValue(ruleId, out var versionEntry))
			version = versionEntry.Version;

		return new DetectionRule
		{
			Authors = TryGetStringArray(rule, "author"),
			Description = rule.GetString("description"),
			Type = rule.GetString("type"),
			Language = TryGetString(rule, "language"),
			License = rule.GetString("license"),
			RiskScore = TryGetInt(rule, "risk_score") ?? 0,
			RuleId = ruleId,
			Severity = rule.GetString("severity"),
			Tags = TryGetStringArray(rule, "tags"),
			Indices = TryGetStringArray(rule, "index"),
			References = TryGetStringArray(rule, "references"),
			IndicesFromDateMath = TryGetString(rule, "from"),
			Setup = TryGetString(rule, "setup"),
			Query = TryGetString(rule, "query"),
			Note = TryGetString(rule, "note"),
			Name = rule.GetString("name"),
			RunsEvery = TryGetString(rule, "interval"),
			MaximumAlertsPerExecution = maxSignals,
			Version = version,
			Threats = threats
		};
	}

	private static DetectionRuleThreat[] GetThreats(TomlTable model)
	{
		if (!model.TryGetValue("threat", out var node) || node is not TomlArray threats)
			return [];

		var threatsList = new List<DetectionRuleThreat>(threats.ArrayValues.Count);
		foreach (var value in threats)
		{
			if (value is not TomlTable threatTable)
				continue;

			var framework = threatTable.GetString("framework");
			var techniques = ReadTechniques(threatTable);

			var tactic = ReadTactic(threatTable);
			var threat = new DetectionRuleThreat
			{
				Framework = framework,
				Techniques = techniques.ToArray(),
				Tactic = tactic
			};
			threatsList.Add(threat);
		}

		return threatsList.ToArray();
	}

	private static IReadOnlyCollection<DetectionRuleTechnique> ReadTechniques(TomlTable threatTable)
	{
		var techniquesArray = threatTable.TryGetValue("technique", out var node) && node is TomlArray ta ? ta : null;
		if (techniquesArray is null)
			return [];
		var techniques = new List<DetectionRuleTechnique>(techniquesArray.Count);
		foreach (var t in techniquesArray)
		{
			if (t is not TomlTable techniqueTable)
				continue;
			var id = techniqueTable.GetString("id");
			var name = techniqueTable.GetString("name");
			var reference = techniqueTable.GetString("reference");
			techniques.Add(new DetectionRuleTechnique
			{
				Id = id,
				Name = name,
				Reference = reference,
				SubTechniques = ReadSubTechniques(techniqueTable).ToArray()
			});
		}
		return techniques;
	}
	private static IReadOnlyCollection<DetectionRuleSubTechnique> ReadSubTechniques(TomlTable techniqueTable)
	{
		var subArray = techniqueTable.TryGetValue("subtechnique", out var node) && node is TomlArray ta ? ta : null;
		if (subArray is null)
			return [];
		var subTechniques = new List<DetectionRuleSubTechnique>(subArray.Count);
		foreach (var t in subArray)
		{
			if (t is not TomlTable subTechniqueTable)
				continue;
			var id = subTechniqueTable.GetString("id");
			var name = subTechniqueTable.GetString("name");
			var reference = subTechniqueTable.GetString("reference");
			subTechniques.Add(new DetectionRuleSubTechnique
			{
				Id = id,
				Name = name,
				Reference = reference
			});
		}
		return subTechniques;
	}

	private static DetectionRuleTactic ReadTactic(TomlTable threatTable)
	{
		var tacticTable = threatTable.GetSubTable("tactic");
		var id = tacticTable.GetString("id");
		var name = tacticTable.GetString("name");
		var reference = tacticTable.GetString("reference");
		return new DetectionRuleTactic
		{
			Id = id,
			Name = name,
			Reference = reference
		};
	}

	private static string[]? TryGetStringArray(TomlTable table, string key) =>
		table.TryGetValue(key, out var node) && node is TomlArray t ? t.ArrayValues.Select(value => value.StringValue).ToArray() : null;

	private static string? TryGetString(TomlTable table, string key) =>
		table.TryGetValue(key, out var node) && node is TomlString t ? t.Value : null;

	private static int? TryGetInt(TomlTable table, string key) =>
		table.TryGetValue(key, out var node) && node is TomlLong t ? (int)t.Value : null;
}
