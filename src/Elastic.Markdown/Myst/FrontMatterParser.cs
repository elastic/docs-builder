// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Markdown.Myst;

[YamlStaticContext]
public partial class YamlFrontMatterStaticContext;

[YamlSerializable]
public class YamlFrontMatter
{
	[YamlMember(Alias = "title")]
	public string? Title { get; set; }

	[YamlMember(Alias = "navigation_title")]
	public string? NavigationTitle { get; set; }

	[YamlMember(Alias = "sub")]
	public Dictionary<string, string>? Properties { get; set; }


	[YamlMember(Alias = "applies")]
	public DeploymentType? AppliesTo { get; set; }
}

[YamlSerializable]
public class DeploymentType
{
	[YamlMember(Alias = "self")]
	public SelfManagedDeployment? SelfManaged { get; set; }

	[YamlMember(Alias = "cloud")]
	public CloudManagedDeployment? Cloud { get; set; }
}

[YamlSerializable]
public class SelfManagedDeployment
{
	[YamlMember(Alias = "stack")]
	public SemVersion? Stack { get; set; }

	[YamlMember(Alias = "ece")]
	public SemVersion? Ece { get; set; }

	[YamlMember(Alias = "eck")]
	public SemVersion? Eck { get; set; }
}

[YamlSerializable]
public class CloudManagedDeployment
{
	[YamlMember(Alias = "ess")]
	public SemVersion? Ess { get; set; }

	[YamlMember(Alias = "serverless")]
	public SemVersion? Serverless { get; set; }
}

public static class FrontMatterParser
{
	public static YamlFrontMatter Deserialize(string yaml)
	{
		var input = new StringReader(yaml);

		var deserializer = new StaticDeserializerBuilder(new YamlFrontMatterStaticContext())
			.IgnoreUnmatchedProperties()
			.WithTypeConverter(new SemVersionConverter())
			.Build();

		var frontMatter = deserializer.Deserialize<YamlFrontMatter>(input);
		return frontMatter;

	}
}

public class SemVersionConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(SemVersion);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var value = parser.Consume<Scalar>();
		return (SemVersion)value.Value;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value == null)
			return;
		emitter.Emit(new Scalar(value.ToString()!));
	}
}
