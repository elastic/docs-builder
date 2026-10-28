// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>
/// One piece of a rendered type annotation. When <see cref="CssClass"/> and <see cref="Title"/>
/// are both null and <see cref="Bare"/> is set the text renders without a wrapping span.
/// <see cref="Href"/> turns that piece into a type-page link; wrappers stay plain text. <see cref="SchemaId"/> is the
/// <c>$ref</c> id of the type the piece names, which links go by, so the text is free to read however it should.
/// <see cref="Lead"/> is a separator that stays on the same line as the piece when a long type wraps.
/// </summary>
public record TypeSpan(
	string Text,
	string? CssClass = null,
	string? Title = null,
	bool Bare = false,
	string? Href = null,
	string? SchemaId = null,
	string? Lead = null
);

/// <summary>
/// The precomputed display form of a schema type (icons, keywords, name), rendered by <c>_SchemaType</c>.
/// </summary>
public record TypeAnnotation(IReadOnlyList<TypeSpan> Spans)
{
	public string Text => string.Concat(Spans.Select(s => s.Lead + s.Text)).Trim();
}
