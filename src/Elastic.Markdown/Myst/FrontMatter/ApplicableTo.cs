// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

[YamlSerializable]
public record ApplicableTo
{
	[YamlMember(Alias = "stack")]
	public AppliesCollection? Stack { get; set; }

	[YamlMember(Alias = "deployment")]
	public DeploymentApplicability? Deployment { get; set; }

	[YamlMember(Alias = "serverless")]
	public ServerlessProjectApplicability? Serverless { get; set; }

	[YamlMember(Alias = "product")]
	public AppliesCollection? Product { get; set; }

	public string[]? Warnings { get; set; }

	public static ApplicableTo All { get; } = new()
	{
		Stack = AppliesCollection.GenerallyAvailable,
		Serverless = ServerlessProjectApplicability.All,
		Deployment = DeploymentApplicability.All,
		Product = AppliesCollection.GenerallyAvailable
	};
}

[YamlSerializable]
public record DeploymentApplicability
{
	[YamlMember(Alias = "self")]
	public AppliesCollection? Self { get; set; }

	[YamlMember(Alias = "ece")]
	public AppliesCollection? Ece { get; set; }

	[YamlMember(Alias = "eck")]
	public AppliesCollection? Eck { get; set; }

	[YamlMember(Alias = "ess")]
	public AppliesCollection? Ess { get; set; }

	public static DeploymentApplicability All { get; } = new()
	{
		Ece = AppliesCollection.GenerallyAvailable,
		Eck = AppliesCollection.GenerallyAvailable,
		Ess = AppliesCollection.GenerallyAvailable,
		Self = AppliesCollection.GenerallyAvailable
	};
}

[YamlSerializable]
public record ServerlessProjectApplicability
{
	[YamlMember(Alias = "elasticsearch")]
	public AppliesCollection? Elasticsearch { get; set; }

	[YamlMember(Alias = "observability")]
	public AppliesCollection? Observability { get; set; }

	[YamlMember(Alias = "security")]
	public AppliesCollection? Security { get; set; }

	/// <summary>
	/// Returns if all projects share the same applicability
	/// </summary>
	public AppliesCollection? AllProjects =>
		Elasticsearch == Observability && Observability == Security
			? Elasticsearch
			: null;

	public static ServerlessProjectApplicability All { get; } = new()
	{
		Elasticsearch = AppliesCollection.GenerallyAvailable,
		Observability = AppliesCollection.GenerallyAvailable,
		Security = AppliesCollection.GenerallyAvailable
	};
}

public class ApplicableToConverter : IYamlTypeConverter
{
	private static readonly string[] KnownKeys =
		["stack", "deployment", "serverless", "product", "ece",
			"eck", "ess", "self", "elasticsearch", "observability","security"
		];

	public bool Accepts(Type type) => type == typeof(ApplicableTo);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		if (parser.TryConsume<Scalar>(out var value))
		{
			if (string.IsNullOrWhiteSpace(value.Value))
				return ApplicableTo.All;
			if (string.Equals(value.Value, "all", StringComparison.InvariantCultureIgnoreCase))
				return ApplicableTo.All;
		}

		var deserialized = rootDeserializer.Invoke(typeof(Dictionary<object, object?>));
		if (deserialized is not Dictionary<object, object?> { Count: > 0 } dictionary)
			return null;


		var applicableTo = new ApplicableTo();
		var warnings = new List<string>();

		var keys = dictionary.Keys.OfType<string>().ToArray();
		var oldStyleKeys = keys.Where(k => k.StartsWith(":")).ToList();
		if (oldStyleKeys.Count > 0)
			warnings.Add($"Applies block does not use valid yaml keys: {string.Join(", ", oldStyleKeys)}");
		var unknownKeys = keys.Except(KnownKeys).Except(oldStyleKeys).ToList();
		if (unknownKeys.Count > 0)
			warnings.Add($"Applies block does not support the following keys: {string.Join(", ", unknownKeys)}");

		if (TryGetApplicabilityOverTime(dictionary, "stack", out var stackAvailability))
			applicableTo.Stack = stackAvailability;

		if (TryGetApplicabilityOverTime(dictionary, "product", out var productAvailability))
			applicableTo.Product = productAvailability;

		AssignServerless(dictionary, applicableTo);
		AssignDeploymentType(dictionary, applicableTo);

		if (TryGetDeployment(dictionary, out var deployment))
			applicableTo.Deployment = deployment;

		if (TryGetProjectApplicability(dictionary, out var serverless))
			applicableTo.Serverless = serverless;

		if (warnings.Count > 0)
			applicableTo.Warnings = warnings.ToArray();
		return applicableTo;
	}

	private static void AssignDeploymentType(Dictionary<object, object?> dictionary, ApplicableTo applicableTo)
	{
		if (!dictionary.TryGetValue("deployment", out var deploymentType))
			return;

		if (deploymentType is null || (deploymentType is string s && string.IsNullOrWhiteSpace(s)))
			applicableTo.Deployment = DeploymentApplicability.All;
		else if (deploymentType is string deploymentTypeString)
		{
			var av = AppliesCollection.TryParse(deploymentTypeString, out var a) ? a : null;
			applicableTo.Deployment = new DeploymentApplicability
			{
				Ece = av,
				Eck = av,
				Ess = av,
				Self = av
			};
		}
		else if (deploymentType is Dictionary<object, object?> deploymentDictionary)
		{
			if (TryGetDeployment(deploymentDictionary, out var applicability))
				applicableTo.Deployment = applicability;
		}
	}

	private static bool TryGetDeployment(Dictionary<object, object?> dictionary, [NotNullWhen(true)] out DeploymentApplicability? applicability)
	{
		applicability = null;
		var d = new DeploymentApplicability();
		var assigned = false;
		if (TryGetApplicabilityOverTime(dictionary, "ece", out var ece))
		{
			d.Ece = ece;
			assigned = true;
		}
		if (TryGetApplicabilityOverTime(dictionary, "eck", out var eck))
		{
			d.Eck = eck;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "ess", out var ess))
		{
			d.Ess = ess;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "self", out var self))
		{
			d.Self = self;
			assigned = true;
		}

		if (assigned)
		{
			applicability = d;
			return true;
		}

		return false;
	}

	private static void AssignServerless(Dictionary<object, object?> dictionary, ApplicableTo applicableTo)
	{
		if (!dictionary.TryGetValue("serverless", out var serverless))
			return;

		if (serverless is null || (serverless is string s && string.IsNullOrWhiteSpace(s)))
			applicableTo.Serverless = ServerlessProjectApplicability.All;
		else if (serverless is string serverlessString)
		{
			var av = AppliesCollection.TryParse(serverlessString, out var a) ? a : null;
			applicableTo.Serverless = new ServerlessProjectApplicability
			{
				Elasticsearch = av,
				Observability = av,
				Security = av
			};
		}
		else if (serverless is Dictionary<object, object?> serverlessDictionary)
		{
			if (TryGetProjectApplicability(serverlessDictionary, out var applicability))
				applicableTo.Serverless = applicability;
		}
	}

	private static bool TryGetProjectApplicability(
		Dictionary<object, object?> dictionary,
		[NotNullWhen(true)] out ServerlessProjectApplicability? applicability
	)
	{
		applicability = null;
		var serverlessAvailability = new ServerlessProjectApplicability();
		var assigned = false;
		if (TryGetApplicabilityOverTime(dictionary, "elasticsearch", out var elasticsearch))
		{
			serverlessAvailability.Elasticsearch = elasticsearch;
			assigned = true;
		}
		if (TryGetApplicabilityOverTime(dictionary, "observability", out var observability))
		{
			serverlessAvailability.Observability = observability;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "security", out var security))
		{
			serverlessAvailability.Security = security;
			assigned = true;
		}

		if (!assigned)
			return false;
		applicability = serverlessAvailability;
		return true;
	}

	private static bool TryGetApplicabilityOverTime(Dictionary<object, object?> dictionary, string key, out AppliesCollection? availability)
	{
		availability = null;
		if (!dictionary.TryGetValue(key, out var target))
			return false;

		if (target is null || (target is string s && string.IsNullOrWhiteSpace(s)))
			availability = AppliesCollection.GenerallyAvailable;
		else if (target is string stackString)
			availability = AppliesCollection.TryParse(stackString, out var a) ? a : null;
		return availability is not null;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}
