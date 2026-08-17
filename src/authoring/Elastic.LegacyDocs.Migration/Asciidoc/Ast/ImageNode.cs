// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.LegacyDocs.Migration.Asciidoc.Ast;

public record ImageNode : IBlockNode
{
	public required string Path { get; init; }
	public string? Alt { get; init; }
	public string? Title { get; init; }
	public string? Width { get; init; }
	public string? Height { get; init; }
}
