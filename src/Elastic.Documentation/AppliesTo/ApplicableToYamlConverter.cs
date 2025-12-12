// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.AppliesTo;

public class ApplicableToYamlConverter(IReadOnlyCollection<string> productKeys) : IYamlTypeConverter
{
	private readonly string[] _knownKeys =
	[
		"stack", "deployment", "serverless", "product", // Applicability categories
		"ece", "eck", "ess", "self", // Deployment options
		"elasticsearch", "observability", "security", // Serverless flavors
		.. productKeys
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

		var keys = dictionary.Keys.OfType<string>().Select(x => x.Replace('_', '-')).ToArray();
		var oldStyleKeys = keys.Where(k => k.StartsWith(':')).ToList();
		if (oldStyleKeys.Count > 0)
			diagnostics.Add((Severity.Warning, $"Applies block does not use valid yaml keys: {string.Join(", ", oldStyleKeys)}"));
		var unknownKeys = keys.Except(_knownKeys).Except(oldStyleKeys).ToList();
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
			{ "apm_agent_rum_js", a => productAvailability.ApmAgentRumJs = a },
			{ "edot_ios", a => productAvailability.EdotIos = a },
			{ "edot_android", a => productAvailability.EdotAndroid = a },
			{ "edot_collector", a => productAvailability.EdotCollector = a },
			{ "edot_dotnet", a => productAvailability.EdotDotnet = a },
			{ "edot_java", a => productAvailability.EdotJava = a },
			{ "edot_node", a => productAvailability.EdotNode = a },
			{ "edot_php", a => productAvailability.EdotPhp = a },
			{ "edot_python", a => productAvailability.EdotPython = a },
			{ "edot_cf_aws", a => productAvailability.EdotCfAws = a },
			{ "edot_cf_azure", a => productAvailability.EdotCfAzure = a },
			{ "edot_cf_gcp", a => productAvailability.EdotCfGcp = a }
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
		{
			availability = AppliesCollection.TryParse(stackString, diagnostics, out var a) ? a : null;

			if (availability is not null)
				ValidateApplicabilityCollection(key, availability, diagnostics);
		}
		return availability is not null;
	}

	private static void ValidateApplicabilityCollection(string key, AppliesCollection collection, List<(Severity, string)> diagnostics)
	{
		var items = collection.ToList();

		// Rule: Only one version declaration per lifecycle
		var lifecycleGroups = items.GroupBy(a => a.Lifecycle).ToList();
		foreach (var group in lifecycleGroups)
		{
			var lifecycleVersionedItems = group.Where(a => a.Version is not null &&
														  a.Version != AllVersionsSpec.Instance).ToList();
			if (lifecycleVersionedItems.Count > 1)
			{
				diagnostics.Add((Severity.Warning,
					$"Key '{key}': Multiple version declarations for {group.Key} lifecycle. Only one version per lifecycle is allowed."));
			}
		}

		// Rule: Only one item per key can use greater-than syntax
		var greaterThanItems = items.Where(a =>
			a.Version is { Kind: VersionSpecKind.GreaterThanOrEqual } &&
			a.Version != AllVersionsSpec.Instance).ToList();

		if (greaterThanItems.Count > 1)
		{
			diagnostics.Add((Severity.Warning,
				$"Key '{key}': Multiple items use greater-than-or-equal syntax. Only one item per key can use this syntax."));
		}

		// Rule: In a range, the first version must be less than or equal the last version
		foreach (var item in items.Where(a => a.Version is { Kind: VersionSpecKind.Range }))
		{
			var spec = item.Version!;
			if (spec.Min.CompareTo(spec.Max!) > 0)
			{
				diagnostics.Add((Severity.Warning,
					$"Key '{key}', {item.Lifecycle}: Range has first version ({spec.Min.Major}.{spec.Min.Minor}) greater than last version ({spec.Max!.Major}.{spec.Max.Minor})."));
			}
		}

		// Rule: No overlapping version ranges for the same key
		var versionedItems = items.Where(a => a.Version is not null &&
											 a.Version != AllVersionsSpec.Instance).ToList();

		for (var i = 0; i < versionedItems.Count; i++)
		{
			for (var j = i + 1; j < versionedItems.Count; j++)
			{
				if (CheckVersionOverlap(versionedItems[i].Version!, versionedItems[j].Version!, out var overlapMsg))
				{
					diagnostics.Add((Severity.Warning,
						$"Key '{key}': Overlapping versions between {versionedItems[i].Lifecycle} and {versionedItems[j].Lifecycle}. {overlapMsg}"));
				}
			}
		}
	}

	private static bool CheckVersionOverlap(VersionSpec v1, VersionSpec v2, out string message)
	{
		message = string.Empty;

		// Get the effective ranges for each version spec
		// For GreaterThanOrEqual: [min, infinity)
		// For Range: [min, max]
		// For Exact: [exact, exact]

		var (v1Min, v1Max) = GetEffectiveRange(v1);
		var (v2Min, v2Max) = GetEffectiveRange(v2);

		var overlaps = v1Min.CompareTo(v2Max ?? new SemVersion(99999, 0, 0)) <= 0 &&
						v2Min.CompareTo(v1Max ?? new SemVersion(99999, 0, 0)) <= 0;

		if (overlaps)
			message = $"Version ranges overlap.";

		return overlaps;
	}

	private static (SemVersion min, SemVersion? max) GetEffectiveRange(VersionSpec spec) => spec.Kind switch
	{
		VersionSpecKind.Exact => (spec.Min, spec.Min),
		VersionSpecKind.Range => (spec.Min, spec.Max),
		VersionSpecKind.GreaterThanOrEqual => (spec.Min, null),
		_ => throw new ArgumentOutOfRangeException(nameof(spec), spec.Kind, "Unknown VersionSpecKind")
	};

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) =>
		serializer.Invoke(value, type);
}
