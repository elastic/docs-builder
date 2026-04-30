// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration.Toc.CliReference;

namespace Elastic.Markdown.Extensions.CliReference;

internal static partial class CliMarkdownGenerator
{
	public static string RootPage(ArghCliSchema schema, string? supplementalContent)
	{
		var sb = new StringBuilder();
		_ = sb.AppendLine($"# {schema.Name}");
		_ = sb.AppendLine();

		if (supplementalContent is not null)
			_ = sb.AppendLine(supplementalContent.Trim());
		else if (!string.IsNullOrWhiteSpace(schema.Description))
			_ = sb.AppendLine(schema.Description.Trim());

		_ = sb.AppendLine();

		if (schema.GlobalOptions.Count > 0)
		{
			_ = sb.AppendLine("## Global Options");
			_ = sb.AppendLine();
			AppendParameters(sb, schema.GlobalOptions);
		}

		var visibleCommands = schema.Commands.Where(c => !c.Hidden).ToList();
		if (visibleCommands.Count > 0)
		{
			_ = sb.AppendLine("## Commands");
			_ = sb.AppendLine();
			foreach (var cmd in visibleCommands)
				AppendPageCard(sb, cmd.Name, $"./{CommandPath(cmd.Name)}.md", cmd.Summary);
		}

		if (schema.Namespaces.Count > 0)
		{
			_ = sb.AppendLine("## Namespaces");
			_ = sb.AppendLine();
			foreach (var ns in schema.Namespaces)
				AppendPageCard(sb, ns.Segment, $"./{ns.Segment}/index.md", ns.Summary);
		}

		return sb.ToString();
	}

	public static string NamespacePage(CliNamespaceSchema ns, string? supplementalContent, string[]? fullPath = null, string? binaryName = null)
	{
		var sb = new StringBuilder();
		var heading = fullPath is { Length: > 0 } ? string.Join(" ", fullPath) : ns.Segment;
		_ = sb.AppendLine($"# {heading} <span class=\"cli-badge-ns\">cli namespace</span>");
		_ = sb.AppendLine();

		// Usage codeblock: binary full-path --help
		_ = sb.AppendLine("```bash");
		_ = sb.AppendLine($"{binaryName ?? heading} {heading} --help");
		_ = sb.AppendLine("```");
		_ = sb.AppendLine();

		if (supplementalContent is not null)
			_ = sb.AppendLine(supplementalContent.Trim());
		else if (!string.IsNullOrWhiteSpace(ns.Summary))
			_ = sb.AppendLine(ns.Summary.Trim());

		_ = sb.AppendLine();

		var visibleCmds = ns.Commands.Where(c => !c.Hidden).ToList();
		if (visibleCmds.Count > 0)
		{
			_ = sb.AppendLine("## Commands");
			_ = sb.AppendLine();
			foreach (var cmd in visibleCmds)
				AppendPageCard(sb, cmd.Name, $"./{CommandPath(cmd.Name)}.md", cmd.Summary);
		}

		if (ns.Namespaces.Count > 0)
		{
			_ = sb.AppendLine("## Sub-namespaces");
			_ = sb.AppendLine();
			foreach (var sub in ns.Namespaces)
				AppendPageCard(sb, sub.Segment, $"./{sub.Segment}/index.md", sub.Summary);
		}

		if (ns.Options.Count > 0)
		{
			_ = sb.AppendLine("## Namespace Flags");
			_ = sb.AppendLine();
			AppendParameters(sb, ns.Options);
		}

		return sb.ToString();
	}

	public static string CommandPage(CliCommandSchema cmd, string? supplementalContent, string[]? fullPath = null, string? binaryName = null)
	{
		var sb = new StringBuilder();
		var heading = fullPath is { Length: > 0 } ? string.Join(" ", fullPath) : cmd.Name;
		_ = sb.AppendLine($"# {heading} <span class=\"cli-badge-cmd\">cli command</span>");
		_ = sb.AppendLine();

		var usage = !string.IsNullOrWhiteSpace(cmd.Usage)
			? cmd.Usage
			: GenerateUsage(cmd, fullPath, binaryName);

		_ = sb.AppendLine("```bash");
		_ = sb.AppendLine(FormatUsage(usage));
		_ = sb.AppendLine("```");
		_ = sb.AppendLine();

		if (supplementalContent is not null)
			_ = sb.AppendLine(supplementalContent.Trim());
		else if (!string.IsNullOrWhiteSpace(cmd.Summary))
			_ = sb.AppendLine(CleanSummary(cmd.Summary).description.Trim());

		_ = sb.AppendLine();

		if (cmd.Parameters.Count > 0)
		{
			var positionals = cmd.Parameters.Where(p => p.Role == "positional").ToList();
			var flags = cmd.Parameters.Where(p => p.Role != "positional").ToList();

			if (positionals.Count > 0)
			{
				_ = sb.AppendLine("## Arguments");
				_ = sb.AppendLine();
				AppendParameters(sb, positionals);
			}

			if (flags.Count > 0)
			{
				_ = sb.AppendLine("## Options");
				_ = sb.AppendLine();
				AppendParameters(sb, flags);
			}
		}

		if (cmd.Examples is { Length: > 0 })
		{
			_ = sb.AppendLine("## Examples");
			_ = sb.AppendLine();
			foreach (var example in cmd.Examples)
			{
				if (string.IsNullOrWhiteSpace(example))
					continue;
				_ = sb.AppendLine("```");
				_ = sb.AppendLine(example.Trim());
				_ = sb.AppendLine("```");
				_ = sb.AppendLine();
			}
		}

		if (!string.IsNullOrWhiteSpace(cmd.Notes))
		{
			_ = sb.AppendLine("## Notes");
			_ = sb.AppendLine();
			_ = sb.AppendLine(cmd.Notes.Trim());
			_ = sb.AppendLine();
		}

		return sb.ToString();
	}

	// Commands named "index" keep cmd- prefix to avoid collision with namespace index.md pages
	private static string CommandPath(string name) =>
		name.Equals("index", StringComparison.OrdinalIgnoreCase) ? $"cmd-{name}" : name;

	private static void AppendPageCard(StringBuilder sb, string title, string url, string? summary)
	{
		var description = string.IsNullOrWhiteSpace(summary) ? string.Empty : CleanSummary(summary).description.Trim();
		_ = sb.AppendLine(":::{page-card} [" + title + "](" + url + ")");
		if (!string.IsNullOrEmpty(description))
			_ = sb.AppendLine(description);
		_ = sb.AppendLine(":::");
		_ = sb.AppendLine();
	}

	private static void AppendParameters(StringBuilder sb, IEnumerable<CliParamSchema> parameters)
	{
		foreach (var p in parameters.Where(p => p.Name != "_" && !p.Hidden))
		{
			var isBool = IsBoolFlag(p.Type);
			var flagName = FormatFlagName(p);
			var typeHint = isBool ? string.Empty : $" `{FormatTypeHint(p)}`";
			var requiredMarker = p.Required ? " **required**" : string.Empty;

			_ = sb.AppendLine($"{flagName}{typeHint}{requiredMarker}");

			// v2: summary may still embed "Values:" / "Default:" for legacy generators;
			// prefer dedicated fields (EnumValues, DefaultValue) when present.
			var (description, legacyValues, legacySummaryDefault) = CleanSummary(p.Summary);

			_ = sb.AppendLine($":   {description.Trim()}");

			// Enum values: prefer schema EnumValues, fall back to legacy embedded text
			var values = p.EnumValues is { Length: > 0 }
				? string.Join(", ", p.EnumValues)
				: legacyValues;

			if (!string.IsNullOrWhiteSpace(values))
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine($"    **Values:** {values.Trim()}");
			}

			// Default: prefer dedicated schema field, fall back to legacy embedded text
			// Skip "default" as a literal value — argh emits this for nullable booleans with no meaningful default
			var defaultValue = (!string.IsNullOrWhiteSpace(p.DefaultValue) && !p.DefaultValue.Equals("default", StringComparison.OrdinalIgnoreCase))
				? p.DefaultValue
				: legacySummaryDefault;
			if (!string.IsNullOrWhiteSpace(defaultValue))
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine($"    **Default:** `{defaultValue.Trim()}`");
			}

			_ = sb.AppendLine();
		}
	}

	private static string GenerateUsage(CliCommandSchema cmd, string[]? fullPath, string? binaryName)
	{
		var parts = new List<string>();
		if (!string.IsNullOrWhiteSpace(binaryName))
			parts.Add(binaryName);
		if (fullPath is { Length: > 0 })
			parts.AddRange(fullPath);
		else
			parts.Add(cmd.Name);

		var visible = cmd.Parameters.Where(p => p.Name != "_" && !p.Hidden).ToList();
		var positionals = visible.Where(p => p.Role == "positional").ToList();
		var requiredFlags = visible.Where(p => p.Role != "positional" && p.Required).ToList();
		var optionalFlags = visible.Where(p => p.Role != "positional" && !p.Required).ToList();

		foreach (var p in positionals)
			parts.Add(p.Required ? $"<{p.Name}>" : $"[<{p.Name}>]");

		foreach (var p in requiredFlags)
		{
			if (IsBoolFlag(p.Type))
				parts.Add($"--{p.Name}");
			else
				parts.Add($"--{p.Name} <{p.Name}>");
		}

		if (optionalFlags.Count > 0)
			parts.Add("[options]");

		return string.Join(" ", parts);
	}

	private static string FormatFlagName(CliParamSchema p)
	{
		if (p.Role == "positional")
			return $"`<{p.Name}>`";

		var isBool = IsBoolFlag(p.Type);
		var prefix = isBool ? "`--[no-]" : "`--";
		var shortPart = p.ShortName is not null ? $"`-{p.ShortName}` " : string.Empty;

		return $"{shortPart}{prefix}{p.Name}`";
	}

	// Parses optional "Values: ..." and "Default: ..." lines that argh embeds in summary text.
	private static (string description, string values, string defaultValue) CleanSummary(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return (string.Empty, string.Empty, string.Empty);

		// Collapse whitespace produced by XML doc indentation (newlines + leading spaces)
		var normalized = WhitespaceRegex().Replace(raw.Trim(), " ");

		// Argh embeds "Values: X, Y. Default: A, B." at the end of summary text.
		// Split on " Values: " first, then on " Default: " within the remainder.
		const string valuesSep = " Values: ";
		const string defaultSep = " Default: ";

		var valuesIdx = normalized.IndexOf(valuesSep, StringComparison.OrdinalIgnoreCase);
		if (valuesIdx < 0)
		{
			// No Values/Default section; check for standalone Default
			var defIdx = normalized.IndexOf(defaultSep, StringComparison.OrdinalIgnoreCase);
			if (defIdx < 0)
				return (normalized, string.Empty, string.Empty);

			return (
				normalized[..defIdx].Trim(),
				string.Empty,
				normalized[(defIdx + defaultSep.Length)..].Trim().TrimEnd('.')
			);
		}

		var description = normalized[..valuesIdx].Trim();
		var remainder = normalized[(valuesIdx + valuesSep.Length)..];

		var defInRemainder = remainder.IndexOf(defaultSep, StringComparison.OrdinalIgnoreCase);
		if (defInRemainder < 0)
			return (description, remainder.Trim().TrimEnd('.'), string.Empty);

		var values = remainder[..defInRemainder].Trim().TrimEnd('.');
		var defaultValue = remainder[(defInRemainder + defaultSep.Length)..].Trim().TrimEnd('.');
		return (description, values, defaultValue);
	}

	// Schema v2 uses JSON Schema primitives: "boolean", "string", "integer", "number", "array", "enum"
	// Schema v1 used "Primitive:bool", "Primitive:bool?", "Primitive" for booleans
	private static bool IsBoolFlag(string type) =>
		type.Equals("boolean", StringComparison.OrdinalIgnoreCase) ||
		type.StartsWith("Primitive:bool", StringComparison.OrdinalIgnoreCase) ||
		type.Equals("Primitive", StringComparison.OrdinalIgnoreCase);

	private static string FormatTypeHint(CliParamSchema p)
	{
		var type = p.Type;

		// v2 JSON Schema primitives
		return type.ToLowerInvariant() switch
		{
			"string" => "string",
			"integer" => "int",
			"number" => "number",
			"boolean" => string.Empty, // shown as --[no-] prefix instead
			"enum" => "enum",
			"array" => p.ElementType switch
			{
				"enum" => "enum[]",
				"integer" => "int[]",
				_ => "string[]"
			},
			// v1 fallback (kind-style strings)
			_ => FormatKindV1(type)
		};
	}

	private static string FormatKindV1(string kind)
	{
		var colon = kind.IndexOf(':');
		var left = colon >= 0 ? kind[..colon] : kind;
		var right = colon >= 0 ? kind[(colon + 1)..] : string.Empty;

		return left switch
		{
			"Collection<enum>" or "Collection<Enum>" => "enum[]",
			"Collection<string>" => "string[]",
			"Collection<int>" or "Collection<Int32>" => "int[]",
			"Enum" => right.Contains('.') ? right[(right.LastIndexOf('.') + 1)..] : right,
			"Primitive" => right switch
			{
				"string" or "string?" => "string",
				"int" or "int?" or "Int32" or "Int32?" => "int",
				_ => string.Empty
			},
			"FileInfo" => "path",
			"DirectoryInfo" => "path",
			_ when left.StartsWith("Collection<") => left["Collection<".Length..].TrimEnd('>') + "[]",
			_ => left
		};
	}

	// Wraps a usage line to multiline bash continuation format when it exceeds 80 chars.
	// Groups flag+value pairs ("--flag <val>") together on the same line.
	private static string FormatUsage(string usage)
	{
		if (usage.Length <= 80)
			return usage;

		var tokens = usage.Split(' ');
		var groups = new List<string>();
		var i = 0;

		// Collect the command prefix (everything before the first flag or bracket)
		var prefixParts = new List<string>();
		while (i < tokens.Length && !tokens[i].StartsWith('-') && !tokens[i].StartsWith('[') && !tokens[i].StartsWith('<'))
		{
			prefixParts.Add(tokens[i]);
			i++;
		}
		groups.Add(string.Join(" ", prefixParts));

		// Group remaining tokens: --flag <value> pairs stay together
		while (i < tokens.Length)
		{
			var token = tokens[i];
			if ((token.StartsWith("--") || (token.StartsWith('-') && token.Length == 2))
				&& i + 1 < tokens.Length
				&& (tokens[i + 1].StartsWith('<') || tokens[i + 1].StartsWith("[<")))
			{
				groups.Add(token + " " + tokens[i + 1]);
				i += 2;
			}
			else
			{
				groups.Add(token);
				i++;
			}
		}

		var result = new StringBuilder();
		_ = result.Append(groups[0]);
		for (var g = 1; g < groups.Count; g++)
		{
			_ = result.Append(" \\");
			_ = result.AppendLine();
			_ = result.Append("  ");
			_ = result.Append(groups[g]);
		}
		return result.ToString();
	}

	[GeneratedRegex(@"\s{2,}")]
	private static partial Regex WhitespaceRegex();
}
