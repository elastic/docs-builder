// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.AppliesTo;

public class ApplicableToYamlConverter : IYamlTypeConverter
{
	private static readonly string[] KnownKeys =
	[
		"stack", "deployment", "serverless", "product",
		"ece", "eck", "ess", "self",
		"elasticsearch", "observability", "security",
		"ecctl", "curator",
		"apm_agent_android","apm_agent_dotnet", "apm_agent_go", "apm_agent_ios", "apm_agent_java", "apm_agent_node", "apm_agent_php", "apm_agent_python", "apm_agent_ruby", "apm_agent_rum",
		"edot_ios", "edot_android", "edot_dotnet", "edot_java", "edot_node", "edot_php", "edot_python", "edot_cf_aws"
	];

	public bool Accepts(Type type) => type == typeof(ApplicableTo);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var diagnostics = new List<(Severity, string)>();
		var applicableTo = new ApplicableTo();

		if (parser.TryConsume<Scalar>(out var value))
		{
			if (string.IsNullOrWhiteSpace(value.Value))
			{
				diagnostics.Add((Severity.Warning, "The 'applies_to' field is present but empty. No applicability will be assumed."));
				return null;
			}

			if (string.Equals(value.Value, "all", StringComparison.OrdinalIgnoreCase))
				return ApplicableTo.All;
		}

		var deserialized = rootDeserializer.Invoke(typeof(Dictionary<object, object?>));
		if (deserialized is not Dictionary<object, object?> { Count: > 0 } dictionary)
			return null;

		var keys = dictionary.Keys.OfType<string>().ToArray();
		var oldStyleKeys = keys.Where(k => k.StartsWith(':')).ToList();
		if (oldStyleKeys.Count > 0)
			diagnostics.Add((Severity.Warning, $"Applies block does not use valid yaml keys: {string.Join(", ", oldStyleKeys)}"));
		var unknownKeys = keys.Except(KnownKeys).Except(oldStyleKeys).ToList();
		if (unknownKeys.Count > 0)
			diagnostics.Add((Severity.Warning, $"Applies block does not support the following keys: {string.Join(", ", unknownKeys)}"));

		if (TryGetApplicabilityOverTime(dictionary, "stack", diagnostics, out var stackAvailability))
			applicableTo.Stack = stackAvailability;

		AssignProduct(dictionary, applicableTo, diagnostics);
		AssignServerless(dictionary, applicableTo, diagnostics);
		AssignDeploymentType(dictionary, applicableTo, diagnostics);

		if (TryGetDeployment(dictionary, diagnostics, out var deployment))
			applicableTo.Deployment = deployment;

		if (TryGetProjectApplicability(dictionary, diagnostics, out var serverless))
			applicableTo.Serverless = serverless;

		if (TryGetProductApplicability(dictionary, diagnostics, out var product))
			applicableTo.ProductApplicability = product;

		if (diagnostics.Count > 0)
			applicableTo.Diagnostics = new ApplicabilityDiagnosticsCollection(diagnostics);
		return applicableTo;
	}

	private static void AssignDeploymentType(Dictionary<object, object?> dictionary, ApplicableTo applicableTo, List<(Severity, string)> diagnostics)
	{
		if (!dictionary.TryGetValue("deployment", out var deploymentType))
			return;

		if (deploymentType is null || (deploymentType is string s && string.IsNullOrWhiteSpace(s)))
			applicableTo.Deployment = DeploymentApplicability.All;
		else if (deploymentType is string deploymentTypeString)
		{
			var av = AppliesCollection.TryParse(deploymentTypeString, diagnostics, out var a) ? a : null;
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
			if (TryGetDeployment(deploymentDictionary, diagnostics, out var applicability))
				applicableTo.Deployment = applicability;
		}
	}

	private static void AssignProduct(Dictionary<object, object?> dictionary, ApplicableTo applicableTo, List<(Severity, string)> diagnostics)
	{
		if (!dictionary.TryGetValue("product", out var productValue))
			return;

		// This handles string, null, and empty string cases.
		if (productValue is not Dictionary<object, object?> productDictionary)
		{
			if (TryGetApplicabilityOverTime(dictionary, "product", diagnostics, out var productAvailability))
				applicableTo.Product = productAvailability;
			return;
		}

		// Handle dictionary case
		if (TryGetProductApplicability(productDictionary, diagnostics, out var applicability))
			applicableTo.ProductApplicability = applicability;
	}

	private static void AssignServerless(Dictionary<object, object?> dictionary, ApplicableTo applicableTo, List<(Severity, string)> diagnostics)
	{
		if (!dictionary.TryGetValue("serverless", out var serverless))
			return;

		if (serverless is null || (serverless is string s && string.IsNullOrWhiteSpace(s)))
			applicableTo.Serverless = ServerlessProjectApplicability.All;
		else if (serverless is string serverlessString)
		{
			var av = AppliesCollection.TryParse(serverlessString, diagnostics, out var a) ? a : null;
			applicableTo.Serverless = new ServerlessProjectApplicability
			{
				Elasticsearch = av,
				Observability = av,
				Security = av
			};
		}
		else if (serverless is Dictionary<object, object?> serverlessDictionary)
		{
			if (TryGetProjectApplicability(serverlessDictionary, diagnostics, out var applicability))
				applicableTo.Serverless = applicability;
		}
	}

	private static bool TryGetDeployment(Dictionary<object, object?> dictionary, List<(Severity, string)> diagnostics,
		[NotNullWhen(true)] out DeploymentApplicability? applicability)
	{
		applicability = null;
		var d = new DeploymentApplicability();
		var assigned = false;

		var mapping = new Dictionary<string, Action<AppliesCollection?>>
		{
			{ "ece", a => d.Ece = a },
			{ "eck", a => d.Eck = a },
			{ "ess", a => d.Ess = a },
			{ "self", a => d.Self = a }
		};

		foreach (var (key, action) in mapping)
		{
			if (!TryGetApplicabilityOverTime(dictionary, key, diagnostics, out var collection))
				continue;
			action(collection);
			assigned = true;
		}

		if (!assigned)
			return false;
		applicability = d;
		return true;
	}

	private static bool TryGetProjectApplicability(Dictionary<object, object?> dictionary,
		List<(Severity, string)> diagnostics,
		[NotNullWhen(true)] out ServerlessProjectApplicability? applicability)
	{
		applicability = null;
		var serverlessAvailability = new ServerlessProjectApplicability();
		var assigned = false;

		var mapping = new Dictionary<string, Action<AppliesCollection?>>
		{
			["elasticsearch"] = a => serverlessAvailability.Elasticsearch = a,
			["observability"] = a => serverlessAvailability.Observability = a,
			["security"] = a => serverlessAvailability.Security = a
		};

		foreach (var (key, action) in mapping)
		{
			if (!TryGetApplicabilityOverTime(dictionary, key, diagnostics, out var collection))
				continue;
			action(collection);
			assigned = true;
		}

		if (!assigned)
			return false;
		applicability = serverlessAvailability;
		return true;
	}

	private static bool TryGetProductApplicability(Dictionary<object, object?> dictionary,
		List<(Severity, string)> diagnostics,
		[NotNullWhen(true)] out ProductApplicability? applicability)
	{
		applicability = null;
		var productAvailability = new ProductApplicability();
		var assigned = false;

		var mapping = new Dictionary<string, Action<AppliesCollection?>>
		{
			{ "ecctl", a => productAvailability.Ecctl = a },
			{ "curator", a => productAvailability.Curator = a },
			{ "apm_agent_android", a => productAvailability.ApmAgentAndroid = a },
			{ "apm_agent_dotnet", a => productAvailability.ApmAgentDotnet = a },
			{ "apm_agent_go", a => productAvailability.ApmAgentGo = a },
			{ "apm_agent_ios", a => productAvailability.ApmAgentIos = a },
			{ "apm_agent_java", a => productAvailability.ApmAgentJava = a },
			{ "apm_agent_node", a => productAvailability.ApmAgentNode = a },
			{ "apm_agent_php", a => productAvailability.ApmAgentPhp = a },
			{ "apm_agent_python", a => productAvailability.ApmAgentPython = a },
			{ "apm_agent_ruby", a => productAvailability.ApmAgentRuby = a },
			{ "apm_agent_rum", a => productAvailability.ApmAgentRum = a },
			{ "edot_ios", a => productAvailability.EdotIos = a },
			{ "edot_android", a => productAvailability.EdotAndroid = a },
			{ "edot_dotnet", a => productAvailability.EdotDotnet = a },
			{ "edot_java", a => productAvailability.EdotJava = a },
			{ "edot_node", a => productAvailability.EdotNode = a },
			{ "edot_php", a => productAvailability.EdotPhp = a },
			{ "edot_python", a => productAvailability.EdotPython = a },
			{ "edot_cf_aws", a => productAvailability.EdotCfAws = a }
		};

		foreach (var (key, action) in mapping)
		{
			if (!TryGetApplicabilityOverTime(dictionary, key, diagnostics, out var collection))
				continue;
			action(collection);
			assigned = true;
		}

		if (!assigned)
			return false;
		applicability = productAvailability;
		return true;
	}

	private static bool TryGetApplicabilityOverTime(Dictionary<object, object?> dictionary, string key, List<(Severity, string)> diagnostics,
		out AppliesCollection? availability)
	{
		availability = null;
		if (!dictionary.TryGetValue(key, out var target))
			return false;

		if (target is null || (target is string s && string.IsNullOrWhiteSpace(s)))
			availability = AppliesCollection.GenerallyAvailable;
		else if (target is string stackString)
			availability = AppliesCollection.TryParse(stackString, diagnostics, out var a) ? a : null;
		return availability is not null;
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}
