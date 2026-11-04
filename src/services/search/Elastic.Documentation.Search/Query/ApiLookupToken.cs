// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#pragma warning disable IDE0130 // 'Query' subfolder would shadow the ES client's Query type
namespace Elastic.Documentation.Search;

public enum ApiLookupTokenKind
{
	/// <summary>A word that may name anything about an operation.</summary>
	Plain,

	/// <summary>A word starting with a slash, with the HTTP method typed before it, which can only name an endpoint.</summary>
	Path
}

/// <summary>
/// One word of an API lookup; a plain word typed with trailing dots asks for the children of the field it names. A path
/// carries the <paramref name="Method"/> typed before it.
/// </summary>
public sealed record ApiLookupToken(ApiLookupTokenKind Kind, string Text, bool ExpandChildren, string? Method = null);
