// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Indexing.Exporters.Elasticsearch;

/// <summary>
/// Describes the metadata fields that can change while a document body stays the same.
/// Feeds the change-detection hash so a metadata-only change rewrites the document.
/// Collections are sorted so a different order alone never counts as a change.
/// </summary>
internal static class DocumentMetadataFingerprint
{
	public static string Create(DocumentationDocument doc) =>
		string.Join(
			"|",
			doc.Product ?? string.Empty,
			Describe(doc.RelatedProducts?.Select(p => $"{p.Id}:{p.Repository}")),
			Describe(doc.Applies?.Select(a => $"{a.Type}:{a.SubType}:{a.Lifecycle}:{a.Version}")),
			doc.Api is { } api ? $"{api.Method} {api.Path} {api.OperationId} {Describe(api.Endpoints)}" : string.Empty
		);

	private static string Describe(IEnumerable<string>? values) =>
		values is null ? string.Empty : string.Join(",", values.Order(StringComparer.Ordinal));
}
