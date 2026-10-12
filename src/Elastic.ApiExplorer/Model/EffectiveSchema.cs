// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// A schema with its <c>allOf</c> members folded in, recursively: the properties, the <c>required</c> names and the map
/// value of the schema and every member. The schema's own entries win over a member's, and earlier members win over later ones.
/// </summary>
/// <param name="Discriminator">
/// The schema's own discriminator, or the one declared on a <c>oneOf</c>/<c>anyOf</c> member of its <c>allOf</c>.
/// </param>
/// <remarks>Built by <see cref="SchemaAnalyzer.Flatten"/>; the only place that walks <c>allOf</c> for structure.</remarks>
public sealed record EffectiveSchema(
	IDictionary<string, IOpenApiSchema> Properties,
	ISet<string> Required,
	IOpenApiSchema? MapValue,
	OpenApiDiscriminator? Discriminator
)
{
	public static readonly EffectiveSchema Empty = new(new Dictionary<string, IOpenApiSchema>(), new HashSet<string>(), null, null);
}
