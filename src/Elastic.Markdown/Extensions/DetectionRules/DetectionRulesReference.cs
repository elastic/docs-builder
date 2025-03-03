// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Elastic.Markdown.IO.Configuration;
using Tomlet;
using Tomlet.Models;

namespace Elastic.Markdown.Extensions.DetectionRules;

public record RulesFolderReference(string Path, bool Found, IReadOnlyCollection<ITocItem> Children) : ITocItem;

public record RuleReference(string Path, bool Found, IReadOnlyCollection<ITocItem> Children, DetectionRule Rule)
	: FileReference(Path, Found, false, Children);


public record DetectionRule
{
	public required string Name { get; init; }

	public required string[]? Authors { get; init; }

	public required string? Note { get; init; }

	public required string? Query { get; init; }

	public required string[]? Tags { get; init; }

	public required string Severity { get; init; }

	public required string RuleId { get; init; }

	public required int RiskScore { get; init; }

	public required string License { get; init; }

	public required string Description { get; init; }
	public required string Type { get; init; }
	public required string? Language { get; init; }
	public required string[]? Indices { get; init; }
	public required string RunsEvery { get; init; }
	public required string? IndicesFromDateMath { get; init; }
	public required string MaximumAlertsPerExecution { get; init; }
	public required string[]? References { get; init; }
	public required string Version { get; init; }

	[UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2075", Justification = "Manually verified")]
	[UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL3050", Justification = "Manually verified")]
	public static DetectionRule From(IFileInfo source)
	{
		TomlDocument model;
		try
		{
			var sourceText = File.ReadAllText(source.FullName);
			model = new TomlParser().Parse(sourceText);
		}
		catch (Exception e)
		{
			throw new Exception($"Could not parse toml in: {source.FullName}", e);
		}
		if (!model.TryGetValue("metadata", out var node) || node is not TomlTable metadata)
			throw new Exception($"Could not find metadata section in {source.FullName}");

		if (!model.TryGetValue("rule", out node) || node is not TomlTable rule)
			throw new Exception($"Could not find rule section in {source.FullName}");

		return new DetectionRule
		{
			Authors = TryGetStringArray(rule, "author"),
			Description = rule.GetString("description"),
			Type = rule.GetString("type"),
			Language = TryGetString(rule, "language"),
			License = rule.GetString("license"),
			RiskScore = TryRead<int>(rule, "risk_score"),
			RuleId = rule.GetString("rule_id"),
			Severity = rule.GetString("severity"),
			Tags = TryGetStringArray(rule, "tags"),
			Indices = TryGetStringArray(rule, "index"),
			References = TryGetStringArray(rule, "references"),
			IndicesFromDateMath = TryGetString(rule, "from"),
			Query = TryGetString(rule, "query"),
			Note = TryGetString(rule, "note"),
			Name = rule.GetString("name"),
			RunsEvery = "?",
			MaximumAlertsPerExecution = "?",
			Version = "?",
		};
	}

	private static string[]? TryGetStringArray(TomlTable table, string key) =>
		table.TryGetValue(key, out var node) && node is TomlArray t ? t.ArrayValues.Select(value => value.StringValue).ToArray() : null;

	private static string? TryGetString(TomlTable table, string key) =>
		table.TryGetValue(key, out var node) && node is TomlString t ? t.Value : null;

	private static TTarget? TryRead<TTarget>(TomlTable table, string key) =>
		table.TryGetValue(key, out var node) && node is TTarget t ? t : default;

	private static TTarget Read<TTarget>(TomlTable table, string key) =>
		TryRead<TTarget>(table, key) ?? throw new Exception($"Could not find {key} in {table}");

}
