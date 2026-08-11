// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Infrastructure;

public sealed record VersionedOpenApiDocument(ResolvedApiVersion Version, OpenApiDocument Document);

/// <summary>
/// Maps stable page identities (operation, tag, schema monikers) to the version monikers
/// where each identity exists across loaded OpenAPI documents.
/// </summary>
public sealed class ApiCrossVersionPageIndex
{
	private readonly Dictionary<(ApiPageVersionTargetKind Kind, string Identity), HashSet<string>> _pages = new();

	public static ApiCrossVersionPageIndex Build(IReadOnlyList<VersionedOpenApiDocument> documents)
	{
		var index = new ApiCrossVersionPageIndex();
		foreach (var versioned in documents)
			index.AddDocument(versioned.Version.Moniker, versioned.Document);

		return index;
	}

	private void AddDocument(string versionMoniker, OpenApiDocument document)
	{
		foreach (var (route, pathItem) in document.Paths ?? [])
		{
			if (pathItem.Operations is null)
				continue;

			foreach (var operation in pathItem.Operations.Values)
			{
				var operationMoniker = ApiUrlBuilder.OperationMoniker(operation.OperationId, route);
				Add(ApiPageVersionTargetKind.Operation, operationMoniker, versionMoniker);
			}
		}

		if (document.Tags is not null)
		{
			foreach (var tag in document.Tags)
			{
				var tagSegment = ApiUrlBuilder.TagMoniker(tag.Name);
				Add(ApiPageVersionTargetKind.Tag, tagSegment, versionMoniker);
			}
		}

		if (document.Components?.Schemas is { } schemas)
		{
			foreach (var schemaId in schemas.Keys)
			{
				var schemaMoniker = ApiUrlBuilder.SchemaMoniker(schemaId);
				Add(ApiPageVersionTargetKind.Schema, schemaMoniker, versionMoniker);
			}
		}
	}

	public bool Contains(ApiPageVersionTarget pageTarget, string versionMoniker) =>
		_pages.TryGetValue((pageTarget.Kind, pageTarget.Identity), out var versions)
		&& versions.Contains(versionMoniker);

	private void Add(ApiPageVersionTargetKind kind, string identity, string versionMoniker)
	{
		var key = (kind, identity);
		if (!_pages.TryGetValue(key, out var versions))
		{
			versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			_pages[key] = versions;
		}

		versions.Add(versionMoniker);
	}
}
