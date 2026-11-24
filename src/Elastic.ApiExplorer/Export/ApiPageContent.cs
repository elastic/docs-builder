// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Export;

/// <summary>
/// One rendered API page: its URL, its title and the hash of its Markdown export. A page other than an operation also
/// carries its search text, plain text read from that export.
/// </summary>
public sealed record ApiPageContent(string Url, string Title, string ContentHash)
{
	public string Description { get; init; } = "";
	public string Body { get; init; } = "";
	public string[] Headings { get; init; } = [];
}
