// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.Settings;
using Elastic.Markdown.Myst.FrontMatter;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Markdown.Myst;

public static class YamlSerialization
{
	public static T Deserialize<T>(string yaml)
	{
		var input = new StringReader(yaml);

		var deserializer = new StaticDeserializerBuilder(new DocsBuilderYamlStaticContext())
			.IgnoreUnmatchedProperties()
			.WithEnumNamingConvention(HyphenatedNamingConvention.Instance)
			.WithTypeConverter(new SemVersionConverter())
			.WithTypeConverter(new ProductConverter())
			.WithTypeConverter(new ApplicableToConverter())
			.Build();

		var frontMatter = deserializer.Deserialize<T>(input);
		return frontMatter;

	}
}

[YamlStaticContext]
[YamlSerializable(typeof(YamlSettings))]
[YamlSerializable(typeof(SettingsGrouping))]
[YamlSerializable(typeof(YamlSettings))]
[YamlSerializable(typeof(SettingsGrouping))]
[YamlSerializable(typeof(Setting))]
[YamlSerializable(typeof(AllowedValue))]
[YamlSerializable(typeof(SettingMutability))]
public partial class DocsBuilderYamlStaticContext;
