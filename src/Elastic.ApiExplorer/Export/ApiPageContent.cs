// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Export;

/// <summary>
/// One rendered API page: its URL, its title and the hash of its Markdown export. <paramref name="Search"/> is set on a page
/// that search should find and that is neither an operation nor the landing page, such as a group, type or topic page.
/// </summary>
public sealed record ApiPageContent(string Url, string Title, string ContentHash, ApiPageSearchText? Search = null);

/// <summary>A page's search text, plain text read from its Markdown export.</summary>
public sealed record ApiPageSearchText(string Title, string Description, string Body, string[] Headings);
