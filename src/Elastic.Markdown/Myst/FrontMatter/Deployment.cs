// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

[YamlSerializable]
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
public record SelfManagedDeployment
{
	[YamlMember(Alias = "stack")]
	public ProductAvailability? Stack { get; set; }

	[YamlMember(Alias = "ece")]
	public ProductAvailability? Ece { get; set; }

	[YamlMember(Alias = "eck")]
	public ProductAvailability? Eck { get; set; }

	public static SelfManagedDeployment All { get; } = new()
	{
		Stack = ProductAvailability.GenerallyAvailable,
		Ece = ProductAvailability.GenerallyAvailable,
		Eck = ProductAvailability.GenerallyAvailable
	};
}

[YamlSerializable]
public record CloudManagedDeployment
{
	[YamlMember(Alias = "hosted")]
	public ProductAvailability? Hosted { get; set; }

	[YamlMember(Alias = "serverless")]
	public ProductAvailability? Serverless { get; set; }

	public static CloudManagedDeployment All { get; } = new()
	{
		Hosted = ProductAvailability.GenerallyAvailable,
		Serverless = ProductAvailability.GenerallyAvailable
	};

}

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
		var x = rootDeserializer.Invoke(typeof(Dictionary<string, string>));
		if (x is not Dictionary<string, string> { Count: > 0 } dictionary)
			return null;

		var deployment = new Deployment();

		if (TryGetVersion("stack", out var version))
		{
			deployment.SelfManaged ??= new SelfManagedDeployment();
			deployment.SelfManaged.Stack = version;
		}
		if (TryGetVersion("ece", out version))
		{
			deployment.SelfManaged ??= new SelfManagedDeployment();
			deployment.SelfManaged.Ece = version;
		}
		if (TryGetVersion("eck", out version))
		{
			deployment.SelfManaged ??= new SelfManagedDeployment();
			deployment.SelfManaged.Eck = version;
		}
		if (TryGetVersion("hosted", out version))
		{
			deployment.Cloud ??= new CloudManagedDeployment();
			deployment.Cloud.Hosted = version;
		}
		if (TryGetVersion("serverless", out version))
		{
			deployment.Cloud ??= new CloudManagedDeployment();
			deployment.Cloud.Serverless = version;
		}
		return deployment;

		bool TryGetVersion(string key, out ProductAvailability? semVersion)
		{
			semVersion = null;
			return dictionary.TryGetValue(key, out var v) && ProductAvailability.TryParse(v, out semVersion);
		}

	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}
