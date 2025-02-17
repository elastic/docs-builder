// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

[YamlSerializable]
[Obsolete( "Use YamlFrontMatter.Apply instead see also DeploymentMode")]
public record Deployment
{
	[YamlMember(Alias = "self")]
	public SelfManagedDeployment? SelfManaged { get; set; }

	[YamlMember(Alias = "cloud")]
	public CloudManagedDeployment? Cloud { get; set; }

	public static Deployment All { get; } = new()
	{
		Cloud = CloudManagedDeployment.All,
		SelfManaged = SelfManagedDeployment.All
	};
}

[YamlSerializable]
[Obsolete( "Use YamlFrontMatter.Apply instead")]
public record SelfManagedDeployment
{
	[YamlMember(Alias = "stack")]
	public Applicability? Stack { get; set; }

	[YamlMember(Alias = "ece")]
	public Applicability? Ece { get; set; }

	[YamlMember(Alias = "eck")]
	public Applicability? Eck { get; set; }

	public static SelfManagedDeployment All { get; } = new()
	{
		Stack = Applicability.GenerallyAvailable,
		Ece = Applicability.GenerallyAvailable,
		Eck = Applicability.GenerallyAvailable
	};
}

[YamlSerializable]
[Obsolete( "Use YamlFrontMatter.Apply instead")]
public record CloudManagedDeployment
{
	[YamlMember(Alias = "hosted")]
	public Applicability? Hosted { get; set; }

	[YamlMember(Alias = "serverless")]
	public Applicability? Serverless { get; set; }

	public static CloudManagedDeployment All { get; } = new()
	{
		Hosted = Applicability.GenerallyAvailable,
		Serverless = Applicability.GenerallyAvailable
	};

}

#pragma warning disable CS0618 // Type or member is obsolete
[Obsolete( "Use DeploymentAvailability instead" )]
public class DeploymentConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(Deployment);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.TryConsume<Scalar>(out var value))
		{
			if (string.IsNullOrWhiteSpace(value.Value))
				return Deployment.All;
			if (string.Equals(value.Value, "all", StringComparison.InvariantCultureIgnoreCase))
				return Deployment.All;
		}
		var deserialized = rootDeserializer.Invoke(typeof(Dictionary<string, string>));
		if (deserialized is not Dictionary<string, string> { Count: > 0 } dictionary)
			return null;

		var deployment = new Deployment();

		if (TryGetAvailability("cloud", out var version))
		{
			deployment.Cloud ??= new CloudManagedDeployment();
			deployment.Cloud.Serverless = version;
			deployment.Cloud.Hosted = version;
		}
		if (TryGetAvailability("self", out version))
		{
			deployment.SelfManaged ??= new SelfManagedDeployment();
			deployment.SelfManaged.Ece = version;
			deployment.SelfManaged.Eck = version;
			deployment.SelfManaged.Stack = version;
		}

		if (TryGetAvailability("stack", out version))
		{
			deployment.SelfManaged ??= new SelfManagedDeployment();
			deployment.SelfManaged.Stack = version;
		}
		if (TryGetAvailability("ece", out version))
		{
			deployment.SelfManaged ??= new SelfManagedDeployment();
			deployment.SelfManaged.Ece = version;
		}
		if (TryGetAvailability("eck", out version))
		{
			deployment.SelfManaged ??= new SelfManagedDeployment();
			deployment.SelfManaged.Eck = version;
		}
		if (TryGetAvailability("hosted", out version))
		{
			deployment.Cloud ??= new CloudManagedDeployment();
			deployment.Cloud.Hosted = version;
		}
		if (TryGetAvailability("serverless", out version))
		{
			deployment.Cloud ??= new CloudManagedDeployment();
			deployment.Cloud.Serverless = version;
		}
		return deployment;

		bool TryGetAvailability(string key, out Applicability? semVersion)
		{
			semVersion = null;
			return dictionary.TryGetValue(key, out var v) && Applicability.TryParse(v, out semVersion);
		}

	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}
#pragma warning restore CS0618 // Type or member is obsolete
