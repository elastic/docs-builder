// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;

namespace Elastic.ApiExplorer.Export;

/// <summary>
/// One rendered API page: its URL, its title and the hash of its Markdown export. <paramref name="Search"/> is set on a page search
/// should find that is neither an operation nor the landing page, such as a group, type or topic page.
/// </summary>
public sealed record ApiPageContent(string Url, string Title, string ContentHash, ApiPageSearch? Search = null);

/// <summary>
/// What search reads of a page: the plain-text title and description of its front matter, and its Markdown export as readers get it.
/// The indexer turns the Markdown into plain text. <paramref name="Properties"/> are the fields a type page lists, found as an operation's are.
/// </summary>
public sealed record ApiPageSearch(string Title, string Description, string Markdown, ApiPropertyEntry[] Properties);
