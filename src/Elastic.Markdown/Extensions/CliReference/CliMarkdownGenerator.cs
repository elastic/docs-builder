// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration.Toc.CliReference;

namespace Elastic.Markdown.Extensions.CliReference;

internal static partial class CliMarkdownGenerator
{
	public static string RootPage(CliSchema schema, CliSupplementalDoc? supplemental)
	{
		var sb = new StringBuilder();
		_ = sb.AppendLine($"# {schema.Name}");
		_ = sb.AppendLine();

		var description = supplemental?.Description ?? schema.Description?.Trim();
		if (!string.IsNullOrWhiteSpace(description))
		{
			_ = sb.AppendLine(description);
			_ = sb.AppendLine();
		}

		if (schema.GlobalOptions.Count > 0)
		{
			_ = sb.AppendLine("## Global Options");
			_ = sb.AppendLine();
			AppendParameters(sb, schema.GlobalOptions, null);
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

		if (schema.Environment?.Variables is { Count: > 0 } envVars)
		{
			_ = sb.AppendLine("## Environment Variables");
			_ = sb.AppendLine();
			foreach (var v in envVars)
			{
				var required = v.Required ? " **required**" : string.Empty;
				_ = sb.AppendLine($"`{v.Name}`{required}");
				_ = sb.AppendLine($":   {v.Description?.Trim() ?? string.Empty}");
				if (!string.IsNullOrWhiteSpace(v.DefaultValue))
				{
					_ = sb.AppendLine();
					_ = sb.AppendLine($"    **Default:** `{v.DefaultValue.Trim()}`");
				}
				_ = sb.AppendLine();
			}
		}

		if (schema.Environment?.ConfigFiles is { Count: > 0 } configFiles)
		{
			_ = sb.AppendLine("## Configuration Files");
			_ = sb.AppendLine();
			foreach (var f in configFiles)
			{
				var required = f.Required ? " **required**" : string.Empty;
				_ = sb.AppendLine($"`{f.Path}`{required}");
				_ = sb.AppendLine($":   {f.Description?.Trim() ?? string.Empty}");
				_ = sb.AppendLine();
			}
		}

		if (!string.IsNullOrWhiteSpace(supplemental?.PostContent))
		{
			_ = sb.AppendLine(supplemental.PostContent.Trim());
			_ = sb.AppendLine();
		}

		return sb.ToString();
	}

	public static string NamespacePage(
		CliNamespaceSchema ns,
		CliSupplementalDoc? supplemental,
		string[]? fullPath = null,
		string? binaryName = null,
		string[]? reservedMetaCommands = null,
		Action<string>? emitError = null)
	{
		var sb = new StringBuilder();
		var heading = fullPath is { Length: > 0 } ? string.Join(" ", fullPath) : ns.Segment;
		_ = sb.AppendLine($"# {heading} <span class=\"cli-badge-ns\">cli namespace</span>");
		_ = sb.AppendLine();

		_ = sb.AppendLine("```bash");
		_ = sb.AppendLine($"{binaryName ?? heading} {heading} --help");
		_ = sb.AppendLine("```");
		_ = sb.AppendLine();

		var description = supplemental?.Description ?? ns.Summary?.Trim();
		if (!string.IsNullOrWhiteSpace(description))
		{
			_ = sb.AppendLine(description);
			_ = sb.AppendLine();
		}

		if (ns.DefaultCommand is { Hidden: false } defaultCmd)
			AppendDefaultCommand(sb, defaultCmd, ns, fullPath, binaryName, reservedMetaCommands);

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
			AppendParameters(sb, ns.Options, supplemental?.OptionOverrides);
		}

		if (supplemental is not null && emitError is not null)
		{
			var nsOptionNames = new HashSet<string>(ns.Options.Select(o => o.Name), StringComparer.OrdinalIgnoreCase);
			foreach (var key in supplemental.OptionOverrides.Keys)
			{
				if (!nsOptionNames.Contains(key))
					emitError($"CLI supplemental: Option '--{key}' not found in namespace '{ns.Segment}'");
			}
		}

		if (!string.IsNullOrWhiteSpace(supplemental?.PostContent))
		{
			_ = sb.AppendLine(supplemental.PostContent.Trim());
			_ = sb.AppendLine();
		}

		return sb.ToString();
	}

	public static string CommandPage(
		CliCommandSchema cmd,
		CliSupplementalDoc? supplemental,
		string[]? fullPath = null,
		string? binaryName = null,
		string[]? reservedMetaCommands = null,
		Action<string>? emitError = null)
	{
		var sb = new StringBuilder();
		var heading = fullPath is { Length: > 0 } ? string.Join(" ", fullPath) : cmd.Name;
		_ = sb.AppendLine($"# {heading} <span class=\"cli-badge-cmd\">cli command</span>");
		_ = sb.AppendLine();

		var usage = !string.IsNullOrWhiteSpace(cmd.Usage)
			? CleanUsage(cmd.Usage, reservedMetaCommands)
			: GenerateUsage(cmd, fullPath, binaryName);

		_ = sb.AppendLine("```bash");
		_ = sb.AppendLine(FormatUsage(usage));
		_ = sb.AppendLine("```");
		_ = sb.AppendLine();

		AppendCommandModifiers(sb, cmd);

		var description = supplemental?.Description ?? (cmd.Summary is not null ? CleanSummary(cmd.Summary).description.Trim() : null);
		if (!string.IsNullOrWhiteSpace(description))
		{
			_ = sb.AppendLine(description);
			_ = sb.AppendLine();
		}

		var behaviorParams = cmd.Parameters
			.Where(p => p.Role is "dryRun" or "confirmationSkip" or "output" && !p.Hidden)
			.ToList();
		if (behaviorParams.Count > 0)
			AppendBehaviorParams(sb, behaviorParams);

		if (cmd.Parameters.Count > 0)
		{
			var positionals = cmd.Parameters.Where(p => p.Role == "positional" && !p.Hidden && p.Name != "_").ToList();
			var flags = cmd.Parameters.Where(p => p.Role != "positional" && !p.Hidden && p.Name != "_").ToList();

			if (positionals.Count > 0)
			{
				_ = sb.AppendLine("## Arguments");
				_ = sb.AppendLine();
				AppendParameters(sb, positionals, supplemental?.ArgumentOverrides);
			}

			if (flags.Count > 0)
			{
				_ = sb.AppendLine("## Options");
				_ = sb.AppendLine();
				AppendParameters(sb, flags, supplemental?.OptionOverrides);
			}
		}

		// Validate supplemental overrides reference real parameters
		if (supplemental is not null && emitError is not null)
		{
			var allNames = new HashSet<string>(cmd.Parameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
			foreach (var key in supplemental.OptionOverrides.Keys)
			{
				if (!allNames.Contains(key))
					emitError($"CLI supplemental: Option '--{key}' not found in command '{cmd.Name}'");
			}
			foreach (var key in supplemental.ArgumentOverrides.Keys)
			{
				if (!allNames.Contains(key))
					emitError($"CLI supplemental: Argument '<{key}>' not found in command '{cmd.Name}'");
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

		if (!string.IsNullOrWhiteSpace(supplemental?.PostContent))
		{
			_ = sb.AppendLine(supplemental.PostContent.Trim());
			_ = sb.AppendLine();
		}

		return sb.ToString();
	}

	private static void AppendCommandModifiers(StringBuilder sb, CliCommandSchema cmd)
	{
		if (cmd.Deprecated is not null)
		{
			var parts = new List<string> { "**Deprecated**" };
			if (!string.IsNullOrWhiteSpace(cmd.Deprecated.Since))
				parts.Add($"since {cmd.Deprecated.Since}");
			if (!string.IsNullOrWhiteSpace(cmd.Deprecated.Message))
				parts.Add(cmd.Deprecated.Message.Trim().TrimEnd('.'));
			if (!string.IsNullOrWhiteSpace(cmd.Deprecated.RemovedIn))
				parts.Add($"Removed in: {cmd.Deprecated.RemovedIn}");
			_ = sb.AppendLine(":::{warning}");
			_ = sb.AppendLine(string.Join(". ", parts) + ".");
			_ = sb.AppendLine(":::");
			_ = sb.AppendLine();
		}

		var hasModifiers = cmd.Intent?.Destructive == true
			|| cmd.Intent?.RequiresConfirmation == true
			|| cmd.Intent?.RequiresAuth == true
			|| cmd.Intent?.Idempotent == true
			|| !string.IsNullOrWhiteSpace(cmd.Intent?.Scope)
			|| cmd.Streaming
			|| cmd.LongRunning;

		if (!hasModifiers)
			return;

		_ = sb.AppendLine(":::{cli-modifiers}");
		if (cmd.Intent?.Destructive == true)
			_ = sb.AppendLine(":destructive:");
		if (cmd.Intent?.RequiresConfirmation == true)
			_ = sb.AppendLine(":requires-confirmation:");
		if (cmd.Intent?.RequiresAuth == true)
			_ = sb.AppendLine(":requires-auth:");
		if (cmd.Intent?.Idempotent == true)
			_ = sb.AppendLine(":idempotent:");
		if (!string.IsNullOrWhiteSpace(cmd.Intent?.Scope))
			_ = sb.AppendLine($":scope: {cmd.Intent.Scope}");
		if (cmd.Streaming)
			_ = sb.AppendLine(":streaming:");
		if (cmd.LongRunning)
			_ = sb.AppendLine(":long-running:");
		_ = sb.AppendLine(":::");
		_ = sb.AppendLine();

		if (cmd.Output?.Formats is { Length: > 0 } formats)
		{
			_ = sb.AppendLine($"**Output formats:** {string.Join(", ", formats)}");
			_ = sb.AppendLine();
		}
	}

	private static void AppendBehaviorParams(StringBuilder sb, List<CliParamSchema> behaviorParams)
	{
		_ = sb.AppendLine("**Behaviour flags:**");
		_ = sb.AppendLine();
		foreach (var p in behaviorParams)
		{
			var flagName = p.Role == "positional" ? $"`<{p.Name}>`" : $"`--{p.Name}`";
			var desc = p.Role switch
			{
				"dryRun" => !string.IsNullOrWhiteSpace(p.Summary) ? CleanSummary(p.Summary).description : "Preview changes without applying them.",
				"confirmationSkip" => !string.IsNullOrWhiteSpace(p.Summary) ? CleanSummary(p.Summary).description : "Skip the confirmation prompt.",
				"output" => !string.IsNullOrWhiteSpace(p.Summary) ? CleanSummary(p.Summary).description : "Control output format.",
				_ => CleanSummary(p.Summary).description
			};
			_ = sb.AppendLine($"{flagName} — {desc.Trim()}");
		}
		_ = sb.AppendLine();
	}

	private static void AppendDefaultCommand(StringBuilder sb, CliDefaultSchema defaultCmd, CliNamespaceSchema ns, string[]? fullPath, string? binaryName, string[]? reservedMetaCommands)
	{
		_ = sb.AppendLine("## Running without a subcommand");
		_ = sb.AppendLine();

		// If Kind matches a named command, emit an alias note instead of duplicating parameters
		if (!string.IsNullOrWhiteSpace(defaultCmd.Kind) &&
			ns.Commands.Any(c => c.Name.Equals(defaultCmd.Kind, StringComparison.OrdinalIgnoreCase)))
		{
			_ = sb.AppendLine($"> Running without a subcommand is an alias for [{defaultCmd.Kind}](./{CommandPath(defaultCmd.Kind)}.md).");
			_ = sb.AppendLine();
			return;
		}

		if (!string.IsNullOrWhiteSpace(defaultCmd.Summary))
		{
			_ = sb.AppendLine(defaultCmd.Summary.Trim());
			_ = sb.AppendLine();
		}

		var usageParts = new List<string>();
		if (!string.IsNullOrWhiteSpace(binaryName))
			usageParts.Add(binaryName);
		if (fullPath is { Length: > 0 })
			usageParts.AddRange(fullPath);

		var rawUsage = !string.IsNullOrWhiteSpace(defaultCmd.Usage)
			? defaultCmd.Usage
			: string.Join(" ", usageParts) + " [options]";
		var usageLine = CleanUsage(rawUsage, reservedMetaCommands);

		_ = sb.AppendLine("```bash");
		_ = sb.AppendLine(FormatUsage(usageLine));
		_ = sb.AppendLine("```");
		_ = sb.AppendLine();

		if (defaultCmd.Parameters.Count > 0)
		{
			var positionals = defaultCmd.Parameters.Where(p => p.Role == "positional").ToList();
			var flags = defaultCmd.Parameters.Where(p => p.Role != "positional").ToList();

			if (positionals.Count > 0)
			{
				_ = sb.AppendLine("### Arguments");
				_ = sb.AppendLine();
				AppendParameters(sb, positionals, null);
			}

			if (flags.Count > 0)
			{
				_ = sb.AppendLine("### Options");
				_ = sb.AppendLine();
				AppendParameters(sb, flags, null);
			}
		}

		if (defaultCmd.Examples is { Length: > 0 })
		{
			_ = sb.AppendLine("### Examples");
			_ = sb.AppendLine();
			foreach (var example in defaultCmd.Examples)
			{
				if (string.IsNullOrWhiteSpace(example))
					continue;
				_ = sb.AppendLine("```");
				_ = sb.AppendLine(example.Trim());
				_ = sb.AppendLine("```");
				_ = sb.AppendLine();
			}
		}

	}

	private static string CleanUsage(string usage, string[]? reservedMetaCommands)
	{
		if (reservedMetaCommands is null)
			return usage;
		foreach (var reserved in reservedMetaCommands)
			usage = usage.Replace(" " + reserved, string.Empty, StringComparison.Ordinal);
		return usage.Trim();
	}

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

	private static void AppendParameters(
		StringBuilder sb,
		IEnumerable<CliParamSchema> parameters,
		Dictionary<string, string>? overrides)
	{
		foreach (var p in parameters.Where(p => p.Name != "_" && !p.Hidden))
		{
			var isBool = IsBoolFlag(p.Type);
			var flagName = FormatFlagName(p);
			var typeHint = isBool ? string.Empty : $" `{FormatTypeHint(p)}`";
			var requiredMarker = p.Required ? " **required**" : string.Empty;

			_ = sb.AppendLine($"{flagName}{typeHint}{requiredMarker}");

			var (description, legacyValues, legacySummaryDefault) = CleanSummary(p.Summary);

			// Use supplemental override if present
			var descLine = overrides is not null && overrides.TryGetValue(p.Name, out var overrideDesc)
				? overrideDesc.Trim()
				: description.Trim();

			// Annotate special roles inline (only when no override)
			if (overrides is null || !overrides.ContainsKey(p.Name))
			{
				if (p.Role == "confirmationSkip")
					descLine = string.IsNullOrEmpty(descLine)
						? "Pass to skip the confirmation prompt."
						: descLine + " (pass to skip the confirmation prompt)";
				else if (p.Role == "dryRun")
					descLine = string.IsNullOrEmpty(descLine)
						? "Preview changes without applying them."
						: descLine + " (preview changes without applying them)";
			}

			_ = sb.AppendLine($":   {descLine}");

			if (p.Deprecated is not null)
			{
				var parts = new List<string> { "**Deprecated**" };
				if (!string.IsNullOrWhiteSpace(p.Deprecated.Since))
					parts.Add($"since {p.Deprecated.Since}");
				if (!string.IsNullOrWhiteSpace(p.Deprecated.Message))
					parts.Add(p.Deprecated.Message.Trim().TrimEnd('.'));
				if (!string.IsNullOrWhiteSpace(p.Deprecated.RemovedIn))
					parts.Add($"Removed in: {p.Deprecated.RemovedIn}");
				_ = sb.AppendLine();
				_ = sb.AppendLine($"    {string.Join(". ", parts)}.");
			}

			var values = p.EnumValues is { Length: > 0 }
				? string.Join(", ", p.EnumValues)
				: legacyValues;

			if (!string.IsNullOrWhiteSpace(values))
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine($"    **Values:** {values.Trim()}");
			}

			var defaultValue = (!string.IsNullOrWhiteSpace(p.DefaultValue) && !p.DefaultValue.Equals("default", StringComparison.OrdinalIgnoreCase))
				? p.DefaultValue
				: legacySummaryDefault;
			if (!string.IsNullOrWhiteSpace(defaultValue))
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine($"    **Default:** `{defaultValue.Trim()}`");
			}

			var constraints = FormatConstraints(p.Validations);
			if (!string.IsNullOrEmpty(constraints))
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine($"    **Constraints:** {constraints}");
			}

			if (p.Repeatable)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine($"    **Repeatable:** pass `--{p.Name}` multiple times to supply more than one value");
			}
			else if (p.Variadic)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine("    **Variadic:** accepts multiple values");
			}

			_ = sb.AppendLine();
		}
	}

	private static string FormatConstraints(List<CliValidationSchema>? validations)
	{
		if (validations is not { Count: > 0 })
			return string.Empty;

		var parts = new List<string>();
		foreach (var v in validations)
		{
			var phrase = v.Kind.ToLowerInvariant() switch
			{
				"existing" => "must exist",
				"rejectsymboliclinks" => "symbolic links not allowed",
				"expanduserprofile" => "supports `~` home expansion",
				"urischeme" when v.Values is { Length: > 0 } =>
					$"must be a {string.Join(" or ", v.Values)} URI",
				"range" when v.Min is not null && v.Max is not null =>
					$"between {v.Min} and {v.Max}",
				"range" when v.Min is not null => $"minimum {v.Min}",
				"range" when v.Max is not null => $"maximum {v.Max}",
				"timespanrange" when v.Min is not null && v.Max is not null =>
					$"duration between {v.Min} and {v.Max}",
				"fileextensions" when v.Values is { Length: > 0 } =>
					$"extensions: {string.Join(", ", v.Values)}",
				"pattern" when v.Pattern is not null => $"must match `{v.Pattern}`",
				_ => null
			};
			if (phrase is not null)
				parts.Add(phrase);
		}

		return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
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

	private static (string description, string values, string defaultValue) CleanSummary(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return (string.Empty, string.Empty, string.Empty);

		var normalized = WhitespaceRegex().Replace(raw.Trim(), " ");

		const string valuesSep = " Values: ";
		const string defaultSep = " Default: ";

		var valuesIdx = normalized.IndexOf(valuesSep, StringComparison.OrdinalIgnoreCase);
		if (valuesIdx < 0)
		{
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

	private static bool IsBoolFlag(string type) =>
		type.Equals("boolean", StringComparison.OrdinalIgnoreCase) ||
		type.StartsWith("Primitive:bool", StringComparison.OrdinalIgnoreCase) ||
		type.Equals("Primitive", StringComparison.OrdinalIgnoreCase);

	private static string FormatTypeHint(CliParamSchema p)
	{
		var type = p.Type;

		return type.ToLowerInvariant() switch
		{
			"string" => "string",
			"integer" => "int",
			"number" => "number",
			"boolean" => string.Empty,
			"enum" => "enum",
			"array" => p.ElementType switch
			{
				"enum" => "enum[]",
				"integer" => "int[]",
				_ => "string[]"
			},
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

	private static string FormatUsage(string usage)
	{
		if (usage.Length <= 80)
			return usage;

		var tokens = usage.Split(' ');
		var groups = new List<string>();
		var i = 0;

		var prefixParts = new List<string>();
		while (i < tokens.Length && !tokens[i].StartsWith('-') && !tokens[i].StartsWith('[') && !tokens[i].StartsWith('<'))
		{
			prefixParts.Add(tokens[i]);
			i++;
		}
		groups.Add(string.Join(" ", prefixParts));

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
