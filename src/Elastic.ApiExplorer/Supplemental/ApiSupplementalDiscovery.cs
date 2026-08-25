// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Supplemental;

public sealed record TagSlugCollision(string Slug, IReadOnlyList<string> TagNames);

public sealed record ApiSupplementalVersionedFile(IFileInfo File, ApiSupplementalFileName Name);

public sealed class ApiSupplementalDiscoveryResult
{
	public static ApiSupplementalDiscoveryResult Empty { get; } = new()
	{
		Operations = new Dictionary<string, IFileInfo>(),
		Tags = new Dictionary<string, IFileInfo>(),
		Unmatched = [],
		Ignored = [],
		VersionSuffixed = [],
		TagSlugCollisions = []
	};

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
		var collisions = TagCollisions(tagNames);
		var collidingSlugs = collisions.Select(c => c.Slug).ToHashSet(StringComparer.Ordinal);
		var tagBySlug = UniqueTagBySlug(tagNames, collidingSlugs);
		var operationSet = operationIds.ToHashSet(StringComparer.Ordinal);

		if (folder is null || !folder.Exists)
		{
			return new ApiSupplementalDiscoveryResult
			{
				Operations = new Dictionary<string, IFileInfo>(),
				Tags = new Dictionary<string, IFileInfo>(),
				Unmatched = [],
				Ignored = [],
				VersionSuffixed = [],
				TagSlugCollisions = collisions
			};
		}

		var operations = new Dictionary<string, IFileInfo>(StringComparer.Ordinal);
		var tags = new Dictionary<string, IFileInfo>(StringComparer.Ordinal);
		var unmatched = new List<IFileInfo>();
		var ignored = new List<IFileInfo>();
		var versionSuffixed = new List<ApiSupplementalVersionedFile>();

		foreach (var file in folder.EnumerateFiles("*.md"))
		{
			if (!ApiSupplementalName.TryParse(file.Name, out var parsed))
			{
				ignored.Add(file);
				continue;
			}

			var name = parsed.Value;
			if (name.IsVersionSuffixed)
			{
				versionSuffixed.Add(new ApiSupplementalVersionedFile(file, name));
				continue;
			}

			if (name.Kind == ApiSupplementalKind.Operation)
			{
				if (operationSet.Contains(name.Stem) && operations.TryAdd(name.Stem, file))
					continue;
				unmatched.Add(file);
				continue;
			}

			if (collidingSlugs.Contains(name.Stem))
			{
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

	public static ApiSupplementalDiscoveryResult Discover(IDirectoryInfo? folder, OpenApiDocument document)
	{
		CollectEntities(document, out var operationIds, out var tagNames);
		return Discover(folder, operationIds, tagNames);
	}

	internal static void CollectEntities(
		OpenApiDocument document,
		out List<string> operationIds,
		out List<string> tagNames)
	{
		var operations = new HashSet<string>(StringComparer.Ordinal);
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
					_ = operations.Add(operation.OperationId);

				if (operation.Tags is null)
					continue;

				foreach (var tagRef in operation.Tags)
				{
					var name = tagRef.Reference?.Id;
					if (!string.IsNullOrEmpty(name))
						_ = tags.Add(name);
				}
			}
		}

		operationIds = [.. operations];
		tagNames = [.. tags];
	}

	private static IReadOnlyList<TagSlugCollision> TagCollisions(IReadOnlyCollection<string> tagNames)
	{
		var bySlug = new Dictionary<string, List<string>>(StringComparer.Ordinal);
		foreach (var tagName in tagNames)
		{
			var slug = ApiSupplementalName.TagFileStem(tagName);
			if (!bySlug.TryGetValue(slug, out var names))
			{
				names = [];
				bySlug[slug] = names;
			}
			if (!names.Contains(tagName, StringComparer.Ordinal))
				names.Add(tagName);
		}

		return [.. bySlug
			.Where(kv => kv.Value.Count > 1)
			.Select(kv => new TagSlugCollision(kv.Key, kv.Value))];
	}

	private static Dictionary<string, string> UniqueTagBySlug(
		IReadOnlyCollection<string> tagNames,
		HashSet<string> collidingSlugs)
	{
		var map = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var tagName in tagNames)
		{
			var slug = ApiSupplementalName.TagFileStem(tagName);
			if (collidingSlugs.Contains(slug))
				continue;
			_ = map.TryAdd(slug, tagName);
		}
		return map;
	}
}
