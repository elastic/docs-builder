// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.LegacyDocs.Migration;

public static class LegacyConfParser
{
	private static readonly IDeserializer Deserializer = new DeserializerBuilder()
		.WithNamingConvention(UnderscoredNamingConvention.Instance)
		.WithTypeConverter(new BranchRefListConverter())
		.WithTypeConverter(new BranchRefConverter())
		.IgnoreUnmatchedProperties()
		.Build();

	public static LegacyConf Parse(string yaml) =>
		Deserializer.Deserialize<LegacyConf>(yaml) ?? new LegacyConf();
}
