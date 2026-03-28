// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.LegacyDocs.Migration;

public static class LegacyConfParser
{
	private static readonly IDeserializer RawDeserializer = new DeserializerBuilder().Build();

	private static readonly ISerializer RoundTripSerializer = new SerializerBuilder()
		.DisableAliases()
		.Build();

	private static readonly IDeserializer TypedDeserializer = new DeserializerBuilder()
		.WithNamingConvention(UnderscoredNamingConvention.Instance)
		.WithTypeConverter(new BranchRefListConverter())
		.WithTypeConverter(new BranchRefConverter())
		.IgnoreUnmatchedProperties()
		.Build();

	public static LegacyConf Parse(string yaml)
	{
		var raw = RawDeserializer.Deserialize<object>(yaml);
		var resolved = RoundTripSerializer.Serialize(raw);
		return TypedDeserializer.Deserialize<LegacyConf>(resolved) ?? new LegacyConf();
	}
}
