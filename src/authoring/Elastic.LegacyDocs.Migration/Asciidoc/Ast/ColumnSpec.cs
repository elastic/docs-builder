// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.LegacyDocs.Migration.Asciidoc.Ast;

public record ColumnSpec
{
	public ColumnHAlign HAlign { get; init; } = ColumnHAlign.Left;
	public ColumnVAlign VAlign { get; init; } = ColumnVAlign.Top;
	public int? Width { get; init; }
	public string? Style { get; init; }
}
