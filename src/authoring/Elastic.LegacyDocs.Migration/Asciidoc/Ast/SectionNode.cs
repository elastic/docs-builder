// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.LegacyDocs.Migration.Asciidoc.Ast;

public record SectionNode : IBlockNode
{
	public required int Level { get; init; }
	public required string Title { get; init; }
	public string? Id { get; init; }
	public List<IAsciidocNode> Children { get; init; } = [];
	/// <summary>True when this section has a [discrete] or [float] block attribute — never becomes its own page.</summary>
	public bool IsDiscrete { get; init; }
	/// <summary>
	/// True when this section was the top-level result of a ProcessInclude call.
	/// Used only for Level-0 sections: a Level-0 section that is NOT an include-root is
	/// the transparent book-root wrapper (becomes the index page); one that IS an include-root
	/// is a standalone part/book and becomes its own page.
	/// </summary>
	public bool IsIncludeRoot { get; init; }
}
