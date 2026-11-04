// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.LegacyDocs.Migration.Asciidoc.Ast;

public record AsciidocDocument : IAsciidocNode
{
	public string? Title { get; init; }
	public string? Id { get; init; }
	public Dictionary<string, string> Attributes { get; init; } = [];
	public List<IAsciidocNode> Children { get; init; } = [];
}
