// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Serialization;

namespace Elastic.Documentation.Links;

public record LinkRegistry
{
	/// Map of branch to <see cref="LinkRegistryEntry"/>
	[JsonPropertyName("repositories")]
	public required Dictionary<string, Dictionary<string, LinkRegistryEntry>> Repositories { get; init; }

	[JsonIgnore]
	public string? ETag { get; init; }

	public LinkRegistry WithLinkRegistryEntry(LinkRegistryEntry entry)
	{
		var copiedRepositories = new Dictionary<string, Dictionary<string, LinkRegistryEntry>>(Repositories);
		var repository = entry.Repository;
		var branch = entry.Branch;
		// repository already exists in links.json
		if (copiedRepositories.TryGetValue(repository, out var existingRepositoryEntry))
		{
			// The branch already exists in the repository entry
			if (existingRepositoryEntry.TryGetValue(branch, out var existingBranchEntry))
			{
				if (entry.UpdatedAt > existingBranchEntry.UpdatedAt)
					existingRepositoryEntry[branch] = entry;
			}
			// branch does not exist in the repository entry
			else
				existingRepositoryEntry[branch] = entry;
		}
		// onboarding new repository
		else
		{
			copiedRepositories.Add(repository, new Dictionary<string, LinkRegistryEntry>
			{
				{ branch, entry }
			});
		}
		return this with { Repositories = copiedRepositories };
	}

	public static LinkRegistry Deserialize(Stream json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkRegistry)!;

	public static LinkRegistry Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkRegistry)!;

	public static string Serialize(LinkRegistry registry) =>
		JsonSerializer.Serialize(registry, SourceGenerationContext.Default.LinkRegistry);
}

public record LinkRegistryEntry
{
	[JsonPropertyName("repository")]
	public required string Repository { get; init; }

	[JsonPropertyName("path")]
	public required string Path { get; init; }

	[JsonPropertyName("branch")]
	public required string Branch { get; init; }

	[JsonPropertyName("etag")]
	public required string ETag { get; init; }

	// TODO can be made required after all doc_sets have published again
	[JsonPropertyName("ref")]
	public string GitReference { get; init; } = "unknown";

	// TODO can be made required after all doc_sets have published again
	[JsonPropertyName("updated_at")]
	public DateTime UpdatedAt { get; init; } = DateTime.MinValue;
}
