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
	public Deployment? AppliesTo { get; set; }
}

[YamlSerializable]
public class Deployment
{
	[YamlMember(Alias = "self")]
	public SelfManagedDeployment? SelfManaged { get; set; }

	[YamlMember(Alias = "cloud")]
	public CloudManagedDeployment? Cloud { get; set; }
}

public class AllVersions() : SemVersion(9999, 9999, 9999)
{
	public static AllVersions Instance { get; } = new ();
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

	public static SelfManagedDeployment All { get; } = new()
	{
		Stack = AllVersions.Instance,
		Ece = AllVersions.Instance,
		Eck = AllVersions.Instance
	};
}

[YamlSerializable]
public class CloudManagedDeployment
{
	[YamlMember(Alias = "hosted")]
	public SemVersion? Hosted { get; set; }

	[YamlMember(Alias = "serverless")]
	public SemVersion? Serverless { get; set; }

	public static CloudManagedDeployment All { get; } = new()
	{
		Hosted = AllVersions.Instance,
		Serverless = AllVersions.Instance
	};

}

public static class FrontMatterParser
{
	public static YamlFrontMatter Deserialize(string yaml)
	{
		var input = new StringReader(yaml);

		var deserializer = new StaticDeserializerBuilder(new YamlFrontMatterStaticContext())
			.IgnoreUnmatchedProperties()
			.WithTypeConverter(new SemVersionConverter())
			.WithTypeConverter(new CloudManagedSerializer())
			.WithTypeConverter(new SelfManagedSerializer())
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
		if (string.IsNullOrWhiteSpace(value.Value))
			return AllVersions.Instance;
		if (string.Equals(value.Value, "all", StringComparison.InvariantCultureIgnoreCase))
			return AllVersions.Instance;
		return (SemVersion)value.Value;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value == null)
			return;
		emitter.Emit(new Scalar(value.ToString()!));
	}

	public static SemVersion? Parse(string? value) =>
		value?.Trim().ToLowerInvariant() switch
		{
			null => AllVersions.Instance,
			"all" => AllVersions.Instance,
			"" => AllVersions.Instance,
			_ => SemVersion.TryParse(value, out var v) ? v : null
		};
}

public class CloudManagedSerializer : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(CloudManagedDeployment);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.TryConsume<Scalar>(out var value))
		{
			if (string.IsNullOrWhiteSpace(value.Value))
				return CloudManagedDeployment.All;
			if (string.Equals(value.Value, "all", StringComparison.InvariantCultureIgnoreCase))
				return CloudManagedDeployment.All;
		}
		var x = rootDeserializer.Invoke(typeof(Dictionary<string, string>));
		if (x is not Dictionary<string, string> { Count: > 0 } dictionary)
			return null;

		var cloudManaged = new CloudManagedDeployment();
		if (dictionary.TryGetValue("hosted", out var v))
			cloudManaged.Hosted = SemVersionConverter.Parse(v);
		if (dictionary.TryGetValue("serverless", out v))
			cloudManaged.Serverless = SemVersionConverter.Parse(v);
		return cloudManaged;

	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}

public class SelfManagedSerializer : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(SelfManagedDeployment);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.TryConsume<Scalar>(out var value))
		{
			if (string.IsNullOrWhiteSpace(value.Value))
				return SelfManagedDeployment.All;
			if (string.Equals(value.Value, "all", StringComparison.InvariantCultureIgnoreCase))
				return SelfManagedDeployment.All;
		}
		var x = rootDeserializer.Invoke(typeof(Dictionary<string, string>));
		if (x is not Dictionary<string, string> { Count: > 0 } dictionary)
			return null;

		var deployment = new SelfManagedDeployment();
		if (dictionary.TryGetValue("stack", out var v))
			deployment.Stack =  SemVersionConverter.Parse(v);
		if (dictionary.TryGetValue("ece", out v))
			deployment.Ece = SemVersionConverter.Parse(v);
		if (dictionary.TryGetValue("eck", out v))
			deployment.Eck = SemVersionConverter.Parse(v);
		return deployment;

	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}
