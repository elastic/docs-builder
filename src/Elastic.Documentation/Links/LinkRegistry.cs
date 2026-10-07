// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Equality;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Serialization;
using Generator.Equals;

namespace Elastic.Documentation.Links;

/// <summary>The link index of all repositories' published manifests (<c>link-index.json</c>), keyed by repository and branch.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>LinkLoader.LoadRegistry</c> in <c>Elastic.Documentation.Configuration</c>, which deserializes <c>link-index.json</c> and checks it once, or the pure <see cref="LinkRegistry.WithLinkRegistryEntry"/>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Repositories</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="LinkRegistryConstraints"/>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with <c>LinkExamples.Registry</c> (tests), an object initializer (<c>new LinkRegistry { Repositories = ... }</c>) or <c>DomainGen.Of&lt;LinkRegistry&gt;()</c> (tests), and <c>with</c> for variations.</para>
/// <para><b>Serialised:</b> <c>[JsonInclude]</c> on the internal setters and <c>[JsonRequired]</c> stand in for the lost <c>required</c>: the serializer cannot reach an internal setter without the first, and a missing key is still a parse error with the second.</para>
/// </remarks>
[Equatable]
public partial record LinkRegistry
{
	/// Map of branch to <see cref="LinkRegistryEntry"/>
	[JsonPropertyName("repositories")]
	[JsonInclude]
	[JsonRequired]
	[CustomEquality(typeof(LinkRegistryEntriesComparer))]
#pragma warning disable GE001 // the whole dictionary is compared by LinkRegistryEntriesComparer; the generator's collection attributes compare nested dictionaries by reference
	public Dictionary<string, Dictionary<string, LinkRegistryEntry>> Repositories { get; internal init; } = default!;
#pragma warning restore GE001

	[JsonIgnore]
	public string? ETag { get; internal init; }

	/// <summary>
	/// The registry with <paramref name="entry"/> filed under its repository and branch: a new repository or branch is added, an existing branch
	/// is replaced only by a strictly newer entry (<see cref="LinkRegistryEntry.UpdatedAt"/>), otherwise the registry is returned as it was.
	/// Neither this registry nor its dictionaries are modified.
	/// </summary>
	/// <remarks>
	/// Derive, then re-check: the result is checked by <see cref="LinkRegistryConstraints.All"/> and only errors are in it (the warnings the registry
	/// carried were reported when it was read). The value rides along when an error is broken, so a publisher that tolerates a registry
	/// it did not write can keep adding to it.
	/// </remarks>
	public Result<LinkRegistry> WithLinkRegistryEntry(LinkRegistryEntry entry)
	{
		var copiedRepositories = Repositories.ToDictionary(r => r.Key, r => new Dictionary<string, LinkRegistryEntry>(r.Value));
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
		return DomainOperation.Derived(LinkRegistryConstraints.All, this with { Repositories = copiedRepositories });
	}
}

/// <summary>One published manifest of a repository and branch in the <see cref="LinkRegistry"/>, with the object key and ETag it was read from.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>LinkLoader.LoadRegistry</c> in <c>Elastic.Documentation.Configuration</c>, which deserializes <c>link-index.json</c> and checks it once, or the pure <see cref="LinkRegistry.WithLinkRegistryEntry"/>, or <see cref="LinkRegistryEntry.FromS3Key"/> for an entry read from an object key.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Repository</c>, <c>Path</c>, <c>Branch</c>, <c>ETag</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="LinkRegistryConstraints"/>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with <c>LinkExamples.Entry</c> (tests) or an object initializer (<c>new LinkRegistryEntry { Repository = ..., Path = ..., Branch = ..., ETag = ... }</c>).</para>
/// <para><b>Serialised:</b> <c>[JsonInclude]</c> on the internal setters and <c>[JsonRequired]</c> stand in for the lost <c>required</c>: the serializer cannot reach an internal setter without the first, and a missing key is still a parse error with the second.</para>
/// </remarks>
public record LinkRegistryEntry
{
	[JsonPropertyName("repository")]
	[JsonInclude]
	[JsonRequired]
	public string Repository { get; internal init; } = default!;

	[JsonPropertyName("path")]
	[JsonInclude]
	[JsonRequired]
	public string Path { get; internal init; } = default!;

	[JsonPropertyName("branch")]
	[JsonInclude]
	[JsonRequired]
	public string Branch { get; internal init; } = default!;

	[JsonPropertyName("etag")]
	[JsonInclude]
	[JsonRequired]
	public string ETag { get; internal init; } = default!;

	// TODO can be made required after all doc_sets have published again
	[JsonPropertyName("ref")]
	[JsonInclude]
	public string GitReference
	{
		get;
		// An explicit "ref": null in the JSON is a legacy entry too, so it reads as 'unknown' like a missing key does; the value is never null.
		internal init => field = value ?? UnknownGitReference;
	} = UnknownGitReference;

	/// <summary>The git ref of a legacy entry that has not republished since the ref was added.</summary>
	public const string UnknownGitReference = "unknown";

	// TODO can be made required after all doc_sets have published again
	[JsonPropertyName("updated_at")]
	[JsonInclude]
	public DateTime UpdatedAt { get; internal init; } = DateTime.MinValue;

	/// <summary>
	/// The entry the link index files for an object an S3 notification names. The key is <c>elastic/{repository}/{branch}/links.json</c>; the
	/// repository and branch are read from it, so the entry is filed where <see cref="LinkRegistryConstraints.RegistryEntryPathMatchesKey"/> expects it.
	/// </summary>
	/// <param name="key">The S3 object key.</param>
	/// <param name="eTag">The object's ETag.</param>
	/// <param name="updatedAt">When the object was written (the event's time).</param>
	/// <param name="gitReference">The git ref the manifest was published from.</param>
	/// <exception cref="ArgumentException">The key has no repository and branch segment.</exception>
	public static LinkRegistryEntry FromS3Key(string key, string eTag, DateTime updatedAt, string gitReference)
	{
		var keyTokens = key.Split('/');
		if (keyTokens.Length < 3)
			throw new ArgumentException($"The key '{key}' is not of the form elastic/<repository>/<branch>/links.json.", nameof(key));
		return new LinkRegistryEntry
		{
			Repository = keyTokens[1],
			Branch = keyTokens[2],
			ETag = eTag,
			Path = key,
			UpdatedAt = updatedAt,
			GitReference = gitReference
		};
	}
}
