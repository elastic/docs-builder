// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Schema;

/// <summary>
/// One piece of a rendered type annotation. When <see cref="CssClass"/> and <see cref="Title"/>
/// are both null and <see cref="Bare"/> is set the text renders without a wrapping span.
/// </summary>
public record TypeSpan(string Text, string? CssClass = null, string? Title = null, bool Bare = false);

/// <summary>
/// The precomputed display form of a schema type (icons, keywords, name), rendered by <c>_SchemaType</c>.
/// </summary>
public record TypeAnnotation(IReadOnlyList<TypeSpan> Spans);
