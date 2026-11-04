// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;

namespace Elastic.ApiExplorer.Tests;

/// <summary>A GET operation with a 200 response, the only shape the navigation tests need.</summary>
internal sealed record SpecOperation(string Path, string OperationId, IReadOnlyList<string> Tags);

internal sealed record SpecTag(string Name, string? DisplayName = null, string? Description = null);

internal sealed record SpecTagGroup(string Name, IReadOnlyList<string> Tags);

/// <summary>
/// Builds a small OpenAPI 3.0.3 document as JSON, so a test states only what it is about:
/// the operations, the tags and how the tags are grouped.
/// </summary>
internal sealed record MinimalSpec(IReadOnlyList<SpecOperation> Operations, IReadOnlyList<SpecTag>? Tags = null)
{
	public IReadOnlyList<SpecTagGroup>? TagGroups { get; init; }
	public string Title { get; init; } = "Test API";
	public string Version { get; init; } = "1.0.0";
	public string ResponseDescription { get; init; } = "Success";

	public string ToJson()
	{
		var paths = new JsonObject();
		foreach (var operation in Operations)
		{
			paths[operation.Path] = new JsonObject
			{
				["get"] = new JsonObject
				{
					["operationId"] = operation.OperationId,
					["tags"] = StringArray(operation.Tags),
					["responses"] = new JsonObject { ["200"] = new JsonObject { ["description"] = ResponseDescription } }
				}
			};
		}

		var root = new JsonObject
		{
			["openapi"] = "3.0.3",
			["info"] = new JsonObject { ["title"] = Title, ["version"] = Version },
			["paths"] = paths,
			["tags"] = new JsonArray([.. (Tags ?? []).Select(TagNode)])
		};
		if (TagGroups is not null)
			root["x-tagGroups"] = new JsonArray([.. TagGroups.Select(GroupNode)]);

		return root.ToJsonString();
	}

	private static JsonArray StringArray(IReadOnlyList<string> values)
	{
		var array = new JsonArray();
		foreach (var value in values)
			array.Add(value);
		return array;
	}

	private static JsonNode TagNode(SpecTag tag)
	{
		var node = new JsonObject { ["name"] = tag.Name };
		if (tag.DisplayName is not null)
			node["x-displayName"] = tag.DisplayName;
		if (tag.Description is not null)
			node["description"] = tag.Description;
		return node;
	}

	private static JsonNode GroupNode(SpecTagGroup group) => new JsonObject { ["name"] = group.Name, ["tags"] = StringArray(group.Tags) };
}
