// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Nodes;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>One Prerequisites row: a mono label plus optional type-style badge (from <c>Label: `value`</c>).</summary>
public sealed record PrerequisiteRow(string Label, string? Badge);

public static class OpenApiXReqAuthParser
{
	public const string ExtensionKey = "x-req-auth";

	public static IReadOnlyList<PrerequisiteRow>? TryGetPrerequisiteRows(
		OpenApiOperation operation,
		ILogger? log,
		string? route,
		string? operationId
	)
	{
		var lines = TryGetPrerequisiteLines(operation, log, route, operationId);
		if (lines is null)
			return null;

		var rows = new List<PrerequisiteRow>(lines.Count);
		foreach (var line in lines)
			rows.Add(ParsePrerequisiteRow(line));
		return rows;
	}

	internal static PrerequisiteRow ParsePrerequisiteRow(string line)
	{
		var trimmed = line.Trim();
		var colon = trimmed.IndexOf(':');
		if (colon <= 0)
			return new PrerequisiteRow(trimmed, null);

		var rest = trimmed[(colon + 1)..].Trim();
		if (rest.Length < 3 || rest[0] != '`' || rest[^1] != '`')
			return new PrerequisiteRow(trimmed, null);

		var badge = rest[1..^1];
		if (badge.Length == 0 || badge.Contains('`', StringComparison.Ordinal))
			return new PrerequisiteRow(trimmed, null);

		return new PrerequisiteRow(trimmed[..colon].Trim(), badge);
	}

	public static IReadOnlyList<string>? TryGetPrerequisiteLines(
		OpenApiOperation operation,
		ILogger? log,
		string? route,
		string? operationId
	)
	{
		if (operation.Extensions is null)
			return null;

		if (!operation.Extensions.TryGetValue(ExtensionKey, out var ext) || ext is not JsonNodeExtension jne)
			return null;

		try
		{
			if (jne.Node is not JsonArray array)
			{
				log?.LogWarning(
					"Failed to parse {Extension} extension for operation {OperationId} on path {Path}: expected a JSON array",
					ExtensionKey,
					operationId,
					route
				);
				return null;
			}

			var list = new List<string>();
			foreach (var node in array)
			{
				if (node is null)
					continue;
				var line = LineFromNode(node);
				if (!string.IsNullOrWhiteSpace(line))
					list.Add(line.Trim());
			}

			if (list.Count == 0)
				return null;

			return list;
		}
		catch (Exception ex)
		{
			log?.LogWarning(
				ex,
				"Failed to parse {Extension} extension for operation {OperationId} on path {Path}",
				ExtensionKey,
				operationId,
				route
			);
			return null;
		}
	}

	private static string LineFromNode(JsonNode node) =>
		node is JsonValue value
			? value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() ?? "" : value.ToString() ?? ""
			: node.ToString() ?? "";
}
