// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Documentation.Assembler;

[YamlStaticContext]
[YamlSerializable(typeof(AssemblyConfiguration))]
[YamlSerializable(typeof(Repository))]
public partial class YamlStaticContext;

public record AssemblyConfiguration
{
	public static AssemblyConfiguration Deserialize(string yaml)
	{
		var input = new StringReader(yaml);

		var deserializer = new StaticDeserializerBuilder(new YamlStaticContext())
			.IgnoreUnmatchedProperties()
			.Build();

		try
		{
			var config = deserializer.Deserialize<AssemblyConfiguration>(input);
			return config;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			Console.WriteLine(e.InnerException);
			throw;
		}
	}

	[YamlMember(Alias = "repos")]
	public Dictionary<string, Repository> Repositories { get; set; } = [];
}

public record Repository
{
	[YamlMember(Alias = "repo")]
	public string Origin { get; set; } = string.Empty;

	[YamlMember(Alias = "branch")]
	public string? Branch { get; set; }

}
