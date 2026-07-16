// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// A single search hit, generic over the document type. <see cref="Document"/> carries the typed
/// hit; <see cref="Title"/> / <see cref="Description"/> are the highlighted snippets ready for
/// display.
/// </summary>
public record SearchResultItem<TDocument> where TDocument : SearchDocumentBase
{
	public required TDocument Document { get; init; }

	/// <summary>The highlighted title (with <c>&lt;mark&gt;</c> tags). Falls back to <see cref="SearchDocumentBase.Title"/>.</summary>
	public required string Title { get; init; }

	/// <summary>The highlighted snippet (with <c>&lt;mark&gt;</c> tags). Falls back to <see cref="SearchDocumentBase.Description"/>.</summary>
	public required string Description { get; init; }

	public float Score { get; init; }
}
