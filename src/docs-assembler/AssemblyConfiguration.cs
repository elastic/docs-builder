// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Documentation.Builder;

[YamlStaticContext]
[YamlSerializable(typeof(AssemblyConfiguration))]
public partial class YamlStaticContext;

public record AssemblyConfiguration
{
	public static AssemblyConfiguration Deserialize(string yaml)
	{
		var input = new StringReader(yaml);

		var deserializer = new StaticDeserializerBuilder(new YamlStaticContext())
			.IgnoreUnmatchedProperties()
			.Build();

		var config = deserializer.Deserialize<AssemblyConfiguration>(input);
		return config;
	}

	[YamlMember(Alias = "repos")]
	public Dictionary<string, string> Repositories { get; set; } = new();
}
