// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Suggestions;
using Elastic.Documentation.Configuration.Versions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

public class ProductConverter(VersionsConfiguration versions) : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(Product);

	public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.Current is Scalar)
		{
			var value = parser.Consume<Scalar>().Value;
			throw new InvalidProductException($"Invalid YAML format. Products must be specified as a mapping with an 'id' field. Found scalar value: '{value}'. Example format:\nproducts:\n  - id: apm", versions);
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
			throw new InvalidProductException("Product 'id' field is required. Example format:\nproducts:\n  - id: apm", versions);

		if (Product.AllById(versions).TryGetValue(productId, out var product))
			return product;

		throw new InvalidProductException(productId, versions);
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) => serializer.Invoke(value, type);
}

public class InvalidProductException(string invalidValue, VersionsConfiguration versions)
	: Exception(
		$"Invalid products frontmatter value: \"{invalidValue}\"." +
		(!string.IsNullOrWhiteSpace(invalidValue) ? " " + new Suggestion(Product.All(versions).Select(p => p.Id).ToHashSet(), invalidValue).GetSuggestionQuestion() : "") +
		"\nYou can find the full list at https://docs-v3-preview.elastic.dev/elastic/docs-builder/tree/main/syntax/frontmatter#products.");
