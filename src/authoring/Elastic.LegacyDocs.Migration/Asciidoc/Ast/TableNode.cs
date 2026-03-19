// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.LegacyDocs.Migration.Asciidoc.Ast;

public record TableNode : IBlockNode
{
	public List<ColumnSpec> Columns { get; init; } = [];
	public List<TableRowNode> HeaderRows { get; init; } = [];
	public List<TableRowNode> BodyRows { get; init; } = [];
}
