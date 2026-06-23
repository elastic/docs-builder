// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Nodes;
using Elastic.Documentation.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

public static class OpenApiXReqAuthParser
{
	public const string ExtensionKey = "x-req-auth";

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
				log?.LogWarning("Failed to parse {Extension} extension for operation {OperationId} on path {Path}: expected a JSON array", ExtensionKey, operationId, route);
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
			log?.LogWarning(ex, "Failed to parse {Extension} extension for operation {OperationId} on path {Path}", ExtensionKey, operationId, route);
			return null;
		}
	}

	private static string LineFromNode(JsonNode node) =>
		node is JsonValue value
			? value.GetValueKind() == JsonValueKind.String
				? value.GetValue<string>() ?? ""
				: value.ToString() ?? ""
			: node.ToString() ?? "";
}
