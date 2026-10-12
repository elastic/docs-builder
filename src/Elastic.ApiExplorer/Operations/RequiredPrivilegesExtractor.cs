// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>One required-privileges row. <paramref name="Join"/> is "or" or "and" when <paramref name="Values"/> has several entries.</summary>
public sealed record PrivilegeRow(string Label, IReadOnlyList<string> Values, string? Join = null);

public sealed record RequiredPrivileges(IReadOnlyList<PrivilegeRow> Rows, string? Description);

/// <summary>
/// Finds the privileges an operation needs. Prefers the <c>x-req-auth</c> extension; otherwise lifts the block that
/// Elasticsearch (<c>## Required authorization</c>) and Kibana (<c>[Required authorization] Route required privileges</c>)
/// write into the description, and returns the description without it.
/// </summary>
public static partial class RequiredPrivilegesExtractor
{
	[GeneratedRegex(@"^## Required authorization[ \t]*\r?\n(?<body>.*?)(?=^#{1,6} |\z)", RegexOptions.Multiline | RegexOptions.Singleline)]
	private static partial Regex ElasticsearchBlock();

	[GeneratedRegex(@"(?:<br\s*/?>\s*)*\[Required authorization\][ \t]*Route required privileges:[ \t]*(?<list>[^\r\n]*?)\.?[ \t]*(?=\r?\n|<br|$)")]
	private static partial Regex KibanaLine();

	[GeneratedRegex(@"`(?<value>[^`]+)`")]
	private static partial Regex BacktickValue();

	public static RequiredPrivileges Extract(OpenApiOperation operation, string? description, ILogger? log, string? route)
	{
		var fromDescription = ExtractFromDescription(description);
		var lines = OpenApiXReqAuthParser.TryGetPrerequisiteLines(operation, log, route, operation.OperationId);
		// The extension is the authoritative source; the description block is still removed so it is not shown twice.
		return lines is { Count: > 0 } ? fromDescription with { Rows = [.. lines.Select(ParseLine)] } : fromDescription;
	}

	internal static RequiredPrivileges ExtractFromDescription(string? description)
	{
		if (string.IsNullOrWhiteSpace(description) || !description.Contains("Required authorization", StringComparison.Ordinal))
			return new RequiredPrivileges([], description);

		var rows = new List<PrivilegeRow>();
		var stripped = ElasticsearchBlock().Replace(description, match => TakeElasticsearch(match, rows));
		stripped = KibanaLine().Replace(stripped, match => TakeKibana(match, rows));
		return rows.Count == 0 ? new RequiredPrivileges([], description) : new RequiredPrivileges(rows, stripped.TrimEnd());
	}

	private static string TakeElasticsearch(Match match, List<PrivilegeRow> rows)
	{
		var found = new List<PrivilegeRow>();
		foreach (var raw in match.Groups["body"].Value.Split('\n'))
		{
			var line = raw.Trim().TrimStart('*', '-').Trim();
			if (line.Length == 0)
				continue;
			if (ParseBulletLine(line) is not { } row)
				return match.Value;
			found.Add(row);
		}

		rows.AddRange(found);
		return "";
	}

	private static PrivilegeRow? ParseBulletLine(string line)
	{
		var colon = line.IndexOf(':');
		if (colon <= 0)
			return null;
		var rest = line[(colon + 1)..];
		var values = BacktickValue().Matches(rest).Select(static m => m.Groups["value"].Value).ToArray();
		// Anything besides the values and their commas (an "or", a scope, a note) would be lost, so the line is not understood.
		var leftover = BacktickValue().Replace(rest, "").Trim(' ', ',', '.');
		return values.Length == 0 || leftover.Length > 0 ? null : new PrivilegeRow(line[..colon].Trim(), values);
	}

	private static string TakeKibana(Match match, List<PrivilegeRow> rows)
	{
		var list = match.Groups["list"].Value;
		var hasOr = list.Contains(" OR ", StringComparison.Ordinal);
		var hasAnd = list.Contains(" AND ", StringComparison.Ordinal);
		if (hasOr && hasAnd)
			return match.Value;

		var separator = hasAnd ? " AND " : " OR ";
		var values = list.Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (values.Length == 0)
			return match.Value;

		rows.Add(new PrivilegeRow("Privileges", values, values.Length > 1 ? separator.Trim().ToLowerInvariant() : null));
		return "";
	}

	/// <summary>An <c>x-req-auth</c> line that is not <c>Label: `value`</c> stays a plain markdown label.</summary>
	private static PrivilegeRow ParseLine(string line) => ParseBulletLine(line.Trim()) ?? new PrivilegeRow(line.Trim(), []);
}
