// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Markdown.Extensions.DetectionRules;

/// <summary>A rule as the overview table lists it: its name, its page URL, and its parsed definition.</summary>
public sealed record DetectionRuleOverviewEntry(string Name, string Url, DetectionRule Rule);

/// <summary>
/// Writes the prebuilt rules overview as one filterable <c>{table}</c>, so readers can search
/// thousands of rules by name and narrow them by domain, severity, rule type, OS, and tactic.
/// </summary>
public static class DetectionRuleOverviewTable
{
	/// <summary>The columns that get a dropdown filter. Rule names are unique, so search covers them.</summary>
	public const string FilterColumns = "Domain, Severity, Rule type, OS, Tactic";

	private static readonly Dictionary<string, string> RuleTypeLabels = new(StringComparer.OrdinalIgnoreCase)
	{
		["query"] = "Custom query",
		["eql"] = "EQL",
		["esql"] = "ES|QL",
		["machine_learning"] = "Machine learning",
		["threshold"] = "Threshold",
		["new_terms"] = "New terms",
		["threat_match"] = "Indicator match"
	};

	public static string ToMarkdown(IEnumerable<DetectionRuleOverviewEntry> entries)
	{
		var sb = new StringBuilder();
		_ = sb.AppendLine(":::{table}");
		_ = sb.AppendLine(":filterable:");
		_ = sb.AppendLine($":filter-columns: {FilterColumns}");
		_ = sb.AppendLine();
		_ = sb.AppendLine("| Rule | Domain | Severity | Rule type | OS | Tactic |");
		_ = sb.AppendLine("| --- | --- | --- | --- | --- | --- |");

		foreach (var entry in entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
		{
			var rule = entry.Rule;
			_ = sb.Append("| [").Append(Cell(entry.Name)).Append("](!").Append(entry.Url).Append(") ");
			_ = sb.Append("| ").Append(Cell(rule.Domain ?? "Unspecified")).Append(' ');
			_ = sb.Append("| ").Append(Cell(Capitalize(rule.Severity))).Append(' ');
			_ = sb.Append("| ").Append(Cell(RuleType(rule.Type))).Append(' ');
			_ = sb.Append("| ").Append(Cell(string.Join(", ", TagValues(rule, "OS")))).Append(' ');
			_ = sb.Append("| ").Append(Cell(string.Join(", ", TagValues(rule, "Tactic")))).AppendLine(" |");
		}

		_ = sb.AppendLine(":::");
		return sb.ToString();
	}

	/// <summary>Values of the tags that look like <c>Prefix: Value</c>, in tag order, without duplicates.</summary>
	public static IReadOnlyList<string> TagValues(DetectionRule rule, string prefix) =>
		[
			.. (rule.Tags ?? [])
				.Where(t => t.StartsWith(prefix + ":", StringComparison.Ordinal))
				.Select(t => t[(prefix.Length + 1)..].Trim())
				.Where(v => v.Length > 0)
				.Distinct(StringComparer.OrdinalIgnoreCase)
		];

	private static string RuleType(string? type) =>
		string.IsNullOrWhiteSpace(type) ? string.Empty : RuleTypeLabels.TryGetValue(type, out var label) ? label : type;

	private static string Capitalize(string? value) =>
		string.IsNullOrEmpty(value) ? string.Empty : char.ToUpperInvariant(value[0]) + value[1..];

	/// <summary>Escapes a value for a pipe-table cell: a literal pipe would end the cell.</summary>
	private static string Cell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
