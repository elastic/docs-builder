// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Suggestions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

public class ProductConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(Product);

	public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.Current is Scalar)
		{
			var value = parser.Consume<Scalar>().Value;
			throw new InvalidProductException($"Invalid YAML format. Products must be specified as a mapping with an 'id' field. Found scalar value: '{value}'. Example format:\nproducts:\n  - id: apm");
		}

		_ = parser.Consume<MappingStart>();
		string? productId = null;

		while (parser.Current is not MappingEnd)
		{
			var key = parser.Consume<Scalar>().Value;
			if (key == "id")
				productId = parser.Consume<Scalar>().Value;
			else
				parser.SkipThisAndNestedEvents();
		}

		_ = parser.Consume<MappingEnd>();

		if (string.IsNullOrWhiteSpace(productId))
			throw new InvalidProductException("Product 'id' field is required. Example format:\nproducts:\n  - id: apm");

		if (Products.AllById.TryGetValue(productId, out var product))
			return product;

		throw new InvalidProductException(productId);
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) => serializer.Invoke(value, type);
}

public class InvalidProductException(string invalidValue)
	: Exception(
		$"Invalid products frontmatter value: \"{invalidValue}\"." +
		(!string.IsNullOrWhiteSpace(invalidValue) ? " " + new Suggestion(ProductExtensions.GetProductIds(), invalidValue).GetSuggestionQuestion() : "") +
		"\nYou can find the full list at https://docs-v3-preview.elastic.dev/elastic/docs-builder/tree/main/syntax/frontmatter#products.");

public static class ProductExtensions
{
	public static IReadOnlySet<string> GetProductIds() =>
		Products.All.Select(p => p.Id).ToFrozenSet();
}
