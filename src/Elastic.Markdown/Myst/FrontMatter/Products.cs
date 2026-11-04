// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Suggestions;
using Elastic.Documentation.Diagnostics;
using SharpYaml.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

public class ProductConverter(ProductsConfiguration products) : YamlConverter<Product>
{
	private static readonly string[] KnownKeys = ["id"];

	public override Product Read(YamlReader reader)
	{
		if (reader.TokenType == YamlTokenType.Scalar)
		{
			var value = reader.ScalarValue;
			_ = reader.Read();
			throw new InvalidProductException(
				$"Invalid YAML format. Products must be specified as a mapping with an 'id' field. Found scalar value: '{value}'. Example format:\nproducts:\n  - id: apm",
				products
			);
		}

		if (reader.TokenType != YamlTokenType.StartMapping)
			throw new InvalidProductException("Product entries must be a mapping with an 'id' field.", products);

		_ = reader.Read(); // consume StartMapping
		string? productId = null;

		while (reader.TokenType != YamlTokenType.EndMapping)
		{
			var key = reader.GetScalarValue();
			_ = reader.Read();
			if (key == "id")
			{
				productId = reader.GetScalarValue();
				_ = reader.Read();
			}
			else
				reader.SkipOrReportUnmapped(typeof(Product), KnownKeys, key, null);
		}
		_ = reader.Read(); // consume EndMapping

		if (string.IsNullOrWhiteSpace(productId))
			throw new InvalidProductException("Product 'id' field is required. Example format:\nproducts:\n  - id: apm", products);

		if (products.PublicReferenceProducts.TryGetValue(productId.Replace('_', '-'), out var product))
			return product;

		throw new InvalidProductException(productId, products);
	}

	public override void Write(YamlWriter writer, Product value) =>
		throw new NotSupportedException("Product YAML serialization is not supported; products front matter is only ever read from YAML.");
}

public class InvalidProductException(string invalidValue, ProductsConfiguration products) : Exception(
	$"Invalid products frontmatter value: \"{invalidValue}\"." +
		(!string.IsNullOrWhiteSpace(invalidValue)
			? " " + new Suggestion(
				products.PublicReferenceProducts.Select(p => p.Value.Id).ToHashSet(),
				invalidValue
			).GetSuggestionQuestion()
			: "") +
		"\nYou can find the full list at https://docs-v3-preview.elastic.dev/elastic/docs-builder/tree/main/syntax/frontmatter#products."
);
