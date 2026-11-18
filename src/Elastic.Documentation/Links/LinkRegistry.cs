// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.Equality;
using Generator.Equals;
using Nullean.Entities;

namespace Elastic.Documentation.Links;

/// <summary>The link index of all repositories' published manifests (<c>link-index.json</c>), keyed by repository and branch.</summary>
[Equatable]
public sealed partial record LinkRegistry : IHaveConstraints<LinkRegistry>
{
	public static ConstraintSet<LinkRegistry> Constraints => LinkRegistryConstraints.All;

	/// Map of branch to <see cref="LinkRegistryEntry"/>
	[JsonPropertyName("repositories")]
	[CustomEquality(typeof(LinkRegistryEntriesComparer))]
#pragma warning disable GE001 // the whole dictionary is compared by LinkRegistryEntriesComparer; the generator's collection attributes compare nested dictionaries by reference
	public required Dictionary<string, Dictionary<string, LinkRegistryEntry>> Repositories { get; init; }
#pragma warning restore GE001

	[JsonIgnore]
	public string? ETag { get; init; }

	/// <summary>
	/// The registry with <paramref name="entry"/> filed under its repository and branch: a new repository or branch is added, an existing branch
	/// is replaced only by a strictly newer entry (<see cref="LinkRegistryEntry.UpdatedAt"/>), otherwise the registry is returned as it was.
	/// </summary>
	/// <remarks>The repository dictionary is copied, the branch dictionaries are not: an update to an existing repository shows through the original.</remarks>
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
			copiedRepositories.Add(repository, new Dictionary<string, LinkRegistryEntry> { { branch, entry } });
		}
		return this with { Repositories = copiedRepositories };
	}
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
	public string GitReference { get; init; } = UnknownGitReference;

	/// <summary>The git ref of a legacy entry that has not republished since the ref was added.</summary>
	public const string UnknownGitReference = "unknown";

	// TODO can be made required after all doc_sets have published again
	[JsonPropertyName("updated_at")]
	public DateTime UpdatedAt { get; init; } = DateTime.MinValue;
}
