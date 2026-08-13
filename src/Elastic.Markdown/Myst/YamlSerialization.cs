// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Markdown.Myst.Directives.Changelog;
using Elastic.Markdown.Myst.Directives.Contributors;
using Elastic.Markdown.Myst.Directives.Settings;
using Elastic.Markdown.Myst.FrontMatter;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Markdown.Myst;

public static class YamlSerialization
{
	public static T Deserialize<T>(string yaml, ProductsConfiguration products)
	{
		var input = new StringReader(yaml);

		var deserializer = new StaticDeserializerBuilder(new DocsBuilderYamlStaticContext())
			.IgnoreUnmatchedProperties()
			.WithEnumNamingConvention(HyphenatedNamingConvention.Instance)
			.WithTypeConverter(new SemVersionConverter())
			.WithTypeConverter(new ProductConverter(products))
			.WithTypeConverter(new ApplicableToYamlConverter(products.PublicReferenceProducts.Keys))
			.WithTypeConverter(new ListingFrontMatterConverter())
			.Build();

		var frontMatter = deserializer.Deserialize<T>(input);
		return frontMatter;
	}
}

/// <summary>
/// Handles both <c>listing: group-name</c> (scalar shorthand) and <c>listing: {group: name}</c> (mapping).
/// </summary>
internal class ListingFrontMatterConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(ListingFrontMatter);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.TryConsume<Scalar>(out var scalar))
			return new ListingFrontMatter { Group = string.IsNullOrWhiteSpace(scalar.Value) ? null : scalar.Value };

		if (!parser.TryConsume<MappingStart>(out _))
			return null;

		var result = new ListingFrontMatter();
		while (!parser.TryConsume<MappingEnd>(out _))
		{
			if (!parser.TryConsume<Scalar>(out var key))
			{
				parser.SkipThisAndNestedEvents();
				continue;
			}
			if (key.Value == "group" && parser.TryConsume<Scalar>(out var val))
				result.Group = string.IsNullOrWhiteSpace(val.Value) ? null : val.Value;
			else
				parser.SkipThisAndNestedEvents();
		}
		return result;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}

[YamlStaticContext]
[YamlSerializable(typeof(YamlSettings))]
[YamlSerializable(typeof(SettingsGrouping))]
[YamlSerializable(typeof(Setting))]
[YamlSerializable(typeof(AllowedValue))]
[YamlSerializable(typeof(SettingMutability))]
[YamlSerializable(typeof(ContributorEntry))]
[YamlSerializable(typeof(ChangelogDirectiveConfigYaml))]
[YamlSerializable(typeof(ChangelogDirectiveBundleConfigYaml))]
[YamlSerializable(typeof(ListingFrontMatter))]
[YamlSerializable(typeof(Elastic.Markdown.Myst.Directives.Hub.LinkCardData))]
[YamlSerializable(typeof(Elastic.Markdown.Myst.Directives.Hub.LinkCardLink))]
public partial class DocsBuilderYamlStaticContext;
