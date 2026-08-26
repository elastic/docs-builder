// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Infrastructure;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Supplemental;

public sealed record TagSlugCollision(string Slug, IReadOnlyList<string> TagNames);

public sealed record ApiSupplementalVersionedFile(IFileInfo File, ApiSupplementalFileName Name);

public sealed class ApiSupplementalDiscoveryResult
{
	public required IReadOnlyDictionary<string, IFileInfo> Operations { get; init; }
	public required IReadOnlyDictionary<string, IFileInfo> Tags { get; init; }
	public required IReadOnlyList<IFileInfo> Unmatched { get; init; }
	public required IReadOnlyList<IFileInfo> Ignored { get; init; }
	public required IReadOnlyList<ApiSupplementalVersionedFile> VersionSuffixed { get; init; }
	public required IReadOnlyList<TagSlugCollision> TagSlugCollisions { get; init; }
}

/// <summary>
/// Discovers top-level <c>op-*.md</c> / <c>tag-*.md</c> files under <c>api/&lt;key&gt;/</c>.
/// Does not emit diagnostics; unmatched convention files are returned for later validation.
/// </summary>
public static class ApiSupplementalDiscovery
{
	public static ApiSupplementalDiscoveryResult Discover(
		IDirectoryInfo? folder,
		IReadOnlyCollection<string> operationIds,
		IReadOnlyCollection<string> tagNames)
	{
		var (tagBySlug, collisions) = IndexTags(tagNames);
		return MatchFiles(folder, operationIds.ToHashSet(StringComparer.Ordinal), tagBySlug, collisions);
	}

	public static ApiSupplementalDiscoveryResult Discover(IDirectoryInfo? folder, OpenApiDocument document)
	{
		var (operationsById, tagNames) = CollectEntities(document);
		var (tagBySlug, collisions) = IndexTags(tagNames);
		return MatchFiles(folder, operationsById.Keys.ToHashSet(StringComparer.Ordinal), tagBySlug, collisions);
	}

	private static ApiSupplementalDiscoveryResult MatchFiles(
		IDirectoryInfo? folder,
		HashSet<string> operationIds,
		Dictionary<string, string> tagBySlug,
		IReadOnlyList<TagSlugCollision> collisions)
	{
		if (folder is null || !folder.Exists)
			return NoFiles(collisions);

		var operations = new Dictionary<string, IFileInfo>(StringComparer.Ordinal);
		var tags = new Dictionary<string, IFileInfo>(StringComparer.Ordinal);
		var unmatched = new List<IFileInfo>();
		var ignored = new List<IFileInfo>();
		var versionSuffixed = new List<ApiSupplementalVersionedFile>();

		foreach (var file in folder.EnumerateFiles("*.md"))
		{
			if (!ApiSupplementalName.TryParse(file.Name, out var name))
			{
				ignored.Add(file);
				continue;
			}

			if (name.IsVersionSuffixed)
			{
				versionSuffixed.Add(new ApiSupplementalVersionedFile(file, name));
				continue;
			}

			if (name.Kind == ApiSupplementalKind.Operation)
			{
				if (operationIds.Contains(name.Stem) && operations.TryAdd(name.Stem, file))
					continue;
				unmatched.Add(file);
				continue;
			}

			if (tagBySlug.TryGetValue(name.Stem, out var tagName) && tags.TryAdd(tagName, file))
				continue;

			unmatched.Add(file);
		}

		return new ApiSupplementalDiscoveryResult
		{
			Operations = operations,
			Tags = tags,
			Unmatched = unmatched,
			Ignored = ignored,
			VersionSuffixed = versionSuffixed,
			TagSlugCollisions = collisions
		};
	}

	internal static (Dictionary<string, OpenApiOperation> OperationsById, HashSet<string> TagNames) CollectEntities(
		OpenApiDocument document)
	{
		var operations = new Dictionary<string, OpenApiOperation>(StringComparer.Ordinal);
		var tags = new HashSet<string>(StringComparer.Ordinal);

		if (document.Tags is not null)
		{
			foreach (var tag in document.Tags)
			{
				if (!string.IsNullOrEmpty(tag.Name))
					_ = tags.Add(tag.Name);
			}
		}

		foreach (var path in document.Paths ?? [])
		{
			if (path.Value.Operations is null)
				continue;

			foreach (var operation in path.Value.Operations.Values)
			{
				if (!string.IsNullOrWhiteSpace(operation.OperationId))
					operations[operation.OperationId] = operation;

				if (operation.Tags is null)
					continue;

				foreach (var tagRef in operation.Tags)
				{
					var name = OperationTagName(tagRef);
					if (!string.IsNullOrEmpty(name))
						_ = tags.Add(name);
				}
			}
		}

		return (operations, tags);
	}

	private static string? OperationTagName(OpenApiTagReference tagRef) =>
		!string.IsNullOrEmpty(tagRef.Name) ? tagRef.Name : tagRef.Reference?.Id;

	private static (Dictionary<string, string> UniqueBySlug, IReadOnlyList<TagSlugCollision> Collisions) IndexTags(
		IReadOnlyCollection<string> tagNames)
	{
		var bySlug = new Dictionary<string, List<string>>(StringComparer.Ordinal);
		foreach (var tagName in tagNames)
		{
			var slug = ApiUrlBuilder.TagSlug(tagName);
			if (!bySlug.TryGetValue(slug, out var names))
			{
				names = [];
				bySlug[slug] = names;
			}

			if (!names.Contains(tagName, StringComparer.Ordinal))
				names.Add(tagName);
		}

		var unique = new Dictionary<string, string>(StringComparer.Ordinal);
		var collisions = new List<TagSlugCollision>();
		foreach (var (slug, names) in bySlug)
		{
			if (names.Count == 1)
				unique[slug] = names[0];
			else
				collisions.Add(new TagSlugCollision(slug, names));
		}

		return (unique, collisions);
	}

	private static ApiSupplementalDiscoveryResult NoFiles(IReadOnlyList<TagSlugCollision> collisions) => new()
	{
		Operations = new Dictionary<string, IFileInfo>(),
		Tags = new Dictionary<string, IFileInfo>(),
		Unmatched = [],
		Ignored = [],
		VersionSuffixed = [],
		TagSlugCollisions = collisions
	};
}
