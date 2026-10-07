// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// <c>allOf: [Base, { oneOf: [A, B] }]</c> means "Base plus one of A or B": the shared base members, and the variants of
/// the union member that each carry them.
/// </summary>
internal sealed record AllOfUnion(IReadOnlyList<IOpenApiSchema> Bases, IList<IOpenApiSchema> Variants, UnionKeyword Keyword)
{
	/// <summary>
	/// Splits an <c>allOf</c> into its union member and the bases around it. The bases only count when they contribute
	/// structure, properties or map values; an <c>allOf</c> that merely wraps a union <c>$ref</c> stays a plain named type.
	/// </summary>
	public static bool TrySplit(IList<IOpenApiSchema> allOf, SchemaAnalyzer analyzer, [NotNullWhen(true)] out AllOfUnion? split)
	{
		split = null;
		foreach (var member in allOf)
		{
			var resolved = analyzer.ResolveSchema(member);
			if (resolved is null || resolved.Enum is { Count: > 0 } || !UnionSchemas.TryGet(resolved, out var keyword, out var variants))
				continue;

			var bases = allOf.Where(m => !ReferenceEquals(m, member)).ToArray();
			if (!bases.Any(b => ContributesStructure(b, analyzer)))
				continue;

			split = new AllOfUnion(bases, variants, keyword);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Folds the bases and one variant into a single object schema. The variant's own properties and map value win over a
	/// base one of the same name, and the <c>required</c> lists of every member carry over.
	/// </summary>
	public MergedVariantSchema MergeInto(IOpenApiSchema variant, SchemaAnalyzer analyzer)
	{
		var properties = new Dictionary<string, IOpenApiSchema>();
		foreach (var member in Bases)
		{
			foreach (var (name, property) in analyzer.GetSchemaProperties(member) ?? new Dictionary<string, IOpenApiSchema>())
				_ = properties.TryAdd(name, property);
		}

		foreach (var (name, property) in analyzer.GetSchemaProperties(variant) ?? new Dictionary<string, IOpenApiSchema>())
			properties[name] = property;

		var required = new HashSet<string>();
		CollectRequired([.. Bases, variant], analyzer, required, [with(ReferenceEqualityComparer.Instance)]);
		return new MergedVariantSchema
		{
			Type = JsonSchemaType.Object,
			Properties = properties,
			Required = required,
			AdditionalProperties = MapValue(variant, analyzer)
				?? Bases.Select(b => MapValue(b, analyzer)).FirstOrDefault(a => a is not null),
			Description = variant.Description
		};
	}

	private static bool ContributesStructure(IOpenApiSchema member, SchemaAnalyzer analyzer) =>
		analyzer.GetSchemaProperties(member)?.Count > 0 || MapValue(member, analyzer) is not null;

	private static IOpenApiSchema? MapValue(IOpenApiSchema member, SchemaAnalyzer analyzer) =>
		(analyzer.ResolveSchema(member) ?? member).AdditionalProperties;

	private static void CollectRequired(
		IEnumerable<IOpenApiSchema> schemas,
		SchemaAnalyzer analyzer,
		HashSet<string> required,
		HashSet<IOpenApiSchema> visited
	)
	{
		foreach (var schema in schemas)
		{
			var resolved = analyzer.ResolveSchema(schema) ?? schema;
			if (!visited.Add(resolved))
				continue;

			if (resolved.Required is { } declared)
				required.UnionWith(declared);
			CollectRequired(resolved.AllOf ?? [], analyzer, required, visited);
		}
	}
}

/// <summary>
/// A variant schema built by <see cref="AllOfUnion.MergeInto"/>. It has no <c>$ref</c> of its own, so code that lists
/// variants keeps the name and <c>$ref</c> of the union option it came from.
/// </summary>
internal sealed class MergedVariantSchema : OpenApiSchema;
