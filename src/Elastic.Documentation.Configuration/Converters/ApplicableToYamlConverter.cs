// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Versions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

// ReSharper disable once CheckNamespace — namespace matches ApplicableTo, not the physical folder
#pragma warning disable IDE0130
namespace Elastic.Documentation.AppliesTo;

public class ApplicableToYamlConverter(IReadOnlyCollection<string> productKeys) : IYamlTypeConverter
{
	private readonly string[] _knownKeys =
	[
		ApplicabilityKeys.Stack,
		ApplicabilityKeys.Deployment,
		ApplicabilityKeys.Serverless,
		ApplicabilityKeys.Product,
		ApplicabilityKeys.Ece,
		ApplicabilityKeys.Eck,
		ApplicabilityKeys.Ess,
		ApplicabilityKeys.Ech,
		ApplicabilityKeys.Self,
		ApplicabilityKeys.Elasticsearch,
		ApplicabilityKeys.Observability,
		ApplicabilityKeys.Security,
		ApplicabilityKeys.VectorDb,
		.. productKeys
	];

	public bool Accepts(Type type) => type == typeof(ApplicableTo);

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var diagnostics = new List<ApplicabilityDiagnostic>();

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

		if (parser.TryConsume<SequenceStart>(out _))
		{
			var merged = new Dictionary<object, object?>();
			while (!parser.TryConsume<SequenceEnd>(out _))
			{
				if (parser.Current is MappingStart)
				{
					var item = rootDeserializer.Invoke(typeof(Dictionary<object, object?>)) as Dictionary<object, object?>;
					if (item is not null)
					{
						foreach (var kv in item)
							merged[kv.Key] = merged.TryGetValue(kv.Key, out var existing)
								? DeepMergeAppliesToNode(existing, kv.Value)
								: kv.Value;
					}
				}
				else if (parser.TryConsume<Scalar>(out var row))
					MergeAppliesToListScalarLine(merged, row.Value, diagnostics);
				else
					_ = parser.MoveNext();
			}

			return merged.Count > 0 ? FinalizeApplicableTo(merged, diagnostics) : null;
		}

		var deserialized = rootDeserializer.Invoke(typeof(Dictionary<object, object?>));
		if (deserialized is not Dictionary<object, object?> { Count: > 0 } dictionary)
			return null;

		return FinalizeApplicableTo(dictionary, diagnostics);
	}

	private static bool ParseCollection(string? value, List<ApplicabilityDiagnostic> diagnostics, out AppliesCollection? availability)
	{
		var parsed = new List<(Severity, string)>();
		var ok = AppliesCollection.TryParse(value, parsed, out availability);
		diagnostics.AddRange(parsed.Select(d => (ApplicabilityDiagnostic)d));
		return ok;
	}

	private static object? DeepMergeAppliesToNode(object? existing, object? incoming)
	{
		if (incoming is not Dictionary<object, object?> incomingDict)
			return incoming;

		if (existing is Dictionary<object, object?> existingDict)
		{
			foreach (var kv in incomingDict)
				existingDict[kv.Key] = existingDict.TryGetValue(kv.Key, out var childExisting)
					? DeepMergeAppliesToNode(childExisting, kv.Value)
					: kv.Value;

			return existingDict;
		}

		return incoming;
	}

	private static void MergeAppliesToListScalarLine(
		Dictionary<object, object?> dictionary,
		string line,
		List<ApplicabilityDiagnostic> diagnostics
	)
	{
		var trimmed = line.Trim();
		var colon = trimmed.IndexOf(':');
		if (colon <= 0 || colon >= trimmed.Length - 1)
		{
			diagnostics.Add((Severity.Warning, $"Applies list item '{line}' could not be parsed as 'key: value'."));
			return;
		}

		var key = trimmed[..colon].Trim();
		var val = trimmed[(colon + 1)..].Trim();
		dictionary[key] = string.IsNullOrEmpty(val) ? null : val;
	}

	private ApplicableTo FinalizeApplicableTo(Dictionary<object, object?> dictionary, List<ApplicabilityDiagnostic> diagnostics)
	{
		var applicableTo = new ApplicableTo();

		var keys = dictionary.Keys.OfType<string>().Select(x => x.Replace('_', '-')).ToArray();
		var oldStyleKeys = keys.Where(k => k.StartsWith(':')).ToList();
		if (oldStyleKeys.Count > 0)
			diagnostics.Add((Severity.Warning, $"Applies block does not use valid yaml keys: {string.Join(", ", oldStyleKeys)}"));
		var unknownKeys = keys.Except(_knownKeys).Except(oldStyleKeys).ToList();
		if (unknownKeys.Count > 0)
			diagnostics.Add((Severity.Warning, $"Applies block does not support the following keys: {string.Join(", ", unknownKeys)}"));

		if (TryGetApplicabilityOverTime(dictionary, ApplicabilityKeys.Stack, diagnostics, out var stackAvailability))
			applicableTo = applicableTo with { Stack = stackAvailability };

		AssignProduct(dictionary, ref applicableTo, diagnostics);
		AssignServerless(dictionary, ref applicableTo, diagnostics);
		AssignDeploymentType(dictionary, ref applicableTo, diagnostics);

		if (TryGetDeployment(dictionary, diagnostics, out var deployment))
			applicableTo = applicableTo with { Deployment = deployment };

		if (TryGetProjectApplicability(dictionary, diagnostics, out var serverless))
			applicableTo = applicableTo with
			{
				Serverless = applicableTo.Serverless is not null
					? MergeServerlessProjectApplicability(applicableTo.Serverless, serverless)
					: serverless
			};

		if (TryGetProductApplicability(dictionary, diagnostics, out var product))
			applicableTo = applicableTo with { ProductApplicability = product };

		if (diagnostics.Count > 0)
			applicableTo = applicableTo with { Diagnostics = new ApplicabilityDiagnosticsCollection(diagnostics) };

		var (unknown, ignored) = RecordDropped(dictionary);
		return applicableTo with { UnknownKeys = unknown, IgnoredValues = ignored };
	}

	private static readonly string[] DeploymentKeys =
	[
		ApplicabilityKeys.Ece,
		ApplicabilityKeys.Eck,
		ApplicabilityKeys.Ess,
		ApplicabilityKeys.Ech,
		ApplicabilityKeys.Self
	];

	private static readonly string[] ProjectKeys =
	[
		ApplicabilityKeys.Elasticsearch,
		ApplicabilityKeys.Observability,
		ApplicabilityKeys.Security,
		ApplicabilityKeys.VectorDb
	];

	private static readonly string[] ProductMappingKeys =
	[
		"ecctl",
		"curator",
		"apm_agent_android",
		"apm_agent_dotnet",
		"apm_agent_go",
		"apm_agent_ios",
		"apm_agent_java",
		"apm_agent_node",
		"apm_agent_php",
		"apm_agent_python",
		"apm_agent_ruby",
		"apm_agent_rum_js",
		"edot_ios",
		"edot_android",
		"edot_collector",
		"edot_dotnet",
		"edot_java",
		"edot_node",
		"edot_browser",
		"edot_php",
		"edot_python",
		"edot_cf_aws",
		"edot_cf_azure",
		"edot_cf_gcp"
	];

	/// <summary>
	/// What the readers above never look at: keys written under <c>deployment</c>, <c>serverless</c> or <c>product</c> that no reader maps
	/// (and top-level keys that pass the known-key check but are not the exact key a reader maps, like <c>apm-agent-java</c>), and keys whose
	/// value is not text. The top-level unknown and old-style keys are reported by <c>Diagnostics</c> and are not repeated here.
	/// </summary>
	private (IReadOnlyList<string> Unknown, IReadOnlyList<string> Ignored) RecordDropped(Dictionary<object, object?> dictionary)
	{
		var unknown = new List<string>();
		var ignored = new List<string>();
		string[] topRead =
		[
			ApplicabilityKeys.Stack,
			ApplicabilityKeys.Deployment,
			ApplicabilityKeys.Serverless,
			ApplicabilityKeys.Product,
			.. DeploymentKeys,
			.. ProjectKeys,
			.. ProductMappingKeys
		];
		foreach (var (k, value) in dictionary)
		{
			if (k is not string key)
				continue;
			if (!topRead.Contains(key, StringComparer.Ordinal))
			{
				if (_knownKeys.Contains(key.Replace('_', '-'), StringComparer.Ordinal))
					unknown.Add(key);
				continue;
			}

			switch (key)
			{
				case ApplicabilityKeys.Deployment:
					RecordContainer(key, value, DeploymentKeys, unknown, ignored);
					break;
				case ApplicabilityKeys.Serverless:
					RecordContainer(key, value, ProjectKeys, unknown, ignored);
					break;
				case ApplicabilityKeys.Product:
					if (value is Dictionary<object, object?>)
						RecordContainer(key, value, ProductMappingKeys, unknown, ignored);
					else if (!IsText(value))
						ignored.Add(key);
					break;
				default:
					if (!IsText(value))
						ignored.Add(key);
					break;
			}
		}

		return (unknown, ignored);
	}

	private static void RecordContainer(string name, object? value, string[] read, List<string> unknown, List<string> ignored)
	{
		if (value is Dictionary<object, object?> nested)
		{
			foreach (var (k, v) in nested)
			{
				if (k is not string key)
					continue;
				if (!read.Contains(key, StringComparer.Ordinal))
					unknown.Add($"{name}.{key}");
				else if (!IsText(v))
					ignored.Add($"{name}.{key}");
			}
		}
		else if (!IsText(value))
			ignored.Add(name);
	}

	private static bool IsText(object? value) => value is null or string;

	private static ServerlessProjectApplicability MergeServerlessProjectApplicability(
		ServerlessProjectApplicability target,
		ServerlessProjectApplicability overrides
	) =>
		target with
		{
			Elasticsearch = overrides.Elasticsearch ?? target.Elasticsearch,
			Observability = overrides.Observability ?? target.Observability,
			Security = overrides.Security ?? target.Security,
			VectorDatabase = overrides.VectorDatabase ?? target.VectorDatabase
		};

	private static void AssignDeploymentType(
		Dictionary<object, object?> dictionary,
		ref ApplicableTo applicableTo,
		List<ApplicabilityDiagnostic> diagnostics
	)
	{
		if (!dictionary.TryGetValue(ApplicabilityKeys.Deployment, out var deploymentType))
			return;

		if (deploymentType is null || (deploymentType is string s && string.IsNullOrWhiteSpace(s)))
			applicableTo = applicableTo with { Deployment = DeploymentApplicability.All };
		else if (deploymentType is string deploymentTypeString)
		{
			var applies = ParseCollection(deploymentTypeString, diagnostics, out var a) ? a : null;
			if (applies is not null)
				ValidateApplicabilityCollection(ApplicabilityKeys.Ess, applies, diagnostics);
			applicableTo = applicableTo with
			{
				Deployment = new DeploymentApplicability { Ece = applies, Eck = applies, Ess = applies, Self = applies }
			};
		}
		else if (deploymentType is Dictionary<object, object?> deploymentDictionary)
		{
			if (TryGetDeployment(deploymentDictionary, diagnostics, out var applicability))
				applicableTo = applicableTo with { Deployment = applicability };
		}
	}

	private static void AssignProduct(
		Dictionary<object, object?> dictionary,
		ref ApplicableTo applicableTo,
		List<ApplicabilityDiagnostic> diagnostics
	)
	{
		if (!dictionary.TryGetValue(ApplicabilityKeys.Product, out var productValue))
			return;

		// This handles string, null, and empty string cases.
		if (productValue is not Dictionary<object, object?> productDictionary)
		{
			if (TryGetApplicabilityOverTime(dictionary, ApplicabilityKeys.Product, diagnostics, out var productAvailability))
				applicableTo = applicableTo with { Product = productAvailability };
			return;
		}

		// Handle dictionary case
		if (TryGetProductApplicability(productDictionary, diagnostics, out var applicability))
			applicableTo = applicableTo with { ProductApplicability = applicability };
	}

	private static void AssignServerless(
		Dictionary<object, object?> dictionary,
		ref ApplicableTo applicableTo,
		List<ApplicabilityDiagnostic> diagnostics
	)
	{
		if (!dictionary.TryGetValue(ApplicabilityKeys.Serverless, out var serverless))
			return;

		if (serverless is null || (serverless is string s && string.IsNullOrWhiteSpace(s)))
			applicableTo = applicableTo with { Serverless = ServerlessProjectApplicability.All };
		else if (serverless is string serverlessString)
		{
			var applies = ParseCollection(serverlessString, diagnostics, out var a) ? a : null;
			if (applies is not null)
				ValidateApplicabilityCollection(ApplicabilityKeys.Serverless, applies, diagnostics);
			applicableTo = applicableTo with
			{
				Serverless = new ServerlessProjectApplicability
				{
					Elasticsearch = applies,
					Observability = applies,
					Security = applies,
					VectorDatabase = applies
				}
			};
		}
		else if (serverless is Dictionary<object, object?> serverlessDictionary)
		{
			if (TryGetProjectApplicability(serverlessDictionary, diagnostics, out var applicability))
				applicableTo = applicableTo with { Serverless = applicability };
		}
	}

	private static bool TryGetDeployment(
		Dictionary<object, object?> dictionary,
		List<ApplicabilityDiagnostic> diagnostics,
		[NotNullWhen(true)] out DeploymentApplicability? applicability
	)
	{
		applicability = null;
		var d = new DeploymentApplicability();
		var assigned = false;

		var hasEss = dictionary.ContainsKey(ApplicabilityKeys.Ess);
		var hasEch = dictionary.ContainsKey(ApplicabilityKeys.Ech);
		if (hasEss && hasEch)
			diagnostics.Add(
				(Severity.Warning, "Both 'ess' and 'ech' are defined. Move 'ess' content into 'ech' to avoid information loss.")
			);

		var mapping = new Dictionary<string, Action<AppliesCollection?>>
		{
			{ ApplicabilityKeys.Ece, a => d = d with { Ece = a } },
			{ ApplicabilityKeys.Eck, a => d = d with { Eck = a } },
			{ ApplicabilityKeys.Ess, a => d = d with { Ess = a } },
			{ ApplicabilityKeys.Ech, a => d = d with { Ess = a } },
			{ ApplicabilityKeys.Self, a => d = d with { Self = a } }
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

	private static bool TryGetProjectApplicability(
		Dictionary<object, object?> dictionary,
		List<ApplicabilityDiagnostic> diagnostics,
		[NotNullWhen(true)] out ServerlessProjectApplicability? applicability
	)
	{
		applicability = null;
		var serverlessAvailability = new ServerlessProjectApplicability();
		var assigned = false;

		var mapping = new Dictionary<string, Action<AppliesCollection?>>
		{
			[ApplicabilityKeys.Elasticsearch] = a => serverlessAvailability = serverlessAvailability with { Elasticsearch = a },
			[ApplicabilityKeys.Observability] = a => serverlessAvailability = serverlessAvailability with { Observability = a },
			[ApplicabilityKeys.Security] = a => serverlessAvailability = serverlessAvailability with { Security = a },
			[ApplicabilityKeys.VectorDb] = a => serverlessAvailability = serverlessAvailability with { VectorDatabase = a }
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

	private static bool TryGetProductApplicability(
		Dictionary<object, object?> dictionary,
		List<ApplicabilityDiagnostic> diagnostics,
		[NotNullWhen(true)] out ProductApplicability? applicability
	)
	{
		applicability = null;
		var productAvailability = new ProductApplicability();
		var assigned = false;

		var mapping = new Dictionary<string, Action<AppliesCollection?>>
		{
			{ "ecctl", a => productAvailability = productAvailability with { Ecctl = a } },
			{ "curator", a => productAvailability = productAvailability with { Curator = a } },
			{ "apm_agent_android", a => productAvailability = productAvailability with { ApmAgentAndroid = a } },
			{ "apm_agent_dotnet", a => productAvailability = productAvailability with { ApmAgentDotnet = a } },
			{ "apm_agent_go", a => productAvailability = productAvailability with { ApmAgentGo = a } },
			{ "apm_agent_ios", a => productAvailability = productAvailability with { ApmAgentIos = a } },
			{ "apm_agent_java", a => productAvailability = productAvailability with { ApmAgentJava = a } },
			{ "apm_agent_node", a => productAvailability = productAvailability with { ApmAgentNode = a } },
			{ "apm_agent_php", a => productAvailability = productAvailability with { ApmAgentPhp = a } },
			{ "apm_agent_python", a => productAvailability = productAvailability with { ApmAgentPython = a } },
			{ "apm_agent_ruby", a => productAvailability = productAvailability with { ApmAgentRuby = a } },
			{ "apm_agent_rum_js", a => productAvailability = productAvailability with { ApmAgentRumJs = a } },
			{ "edot_ios", a => productAvailability = productAvailability with { EdotIos = a } },
			{ "edot_android", a => productAvailability = productAvailability with { EdotAndroid = a } },
			{ "edot_collector", a => productAvailability = productAvailability with { EdotCollector = a } },
			{ "edot_dotnet", a => productAvailability = productAvailability with { EdotDotnet = a } },
			{ "edot_java", a => productAvailability = productAvailability with { EdotJava = a } },
			{ "edot_node", a => productAvailability = productAvailability with { EdotNode = a } },
			{ "edot_browser", a => productAvailability = productAvailability with { EdotBrowser = a } },
			{ "edot_php", a => productAvailability = productAvailability with { EdotPhp = a } },
			{ "edot_python", a => productAvailability = productAvailability with { EdotPython = a } },
			{ "edot_cf_aws", a => productAvailability = productAvailability with { EdotCfAws = a } },
			{ "edot_cf_azure", a => productAvailability = productAvailability with { EdotCfAzure = a } },
			{ "edot_cf_gcp", a => productAvailability = productAvailability with { EdotCfGcp = a } }
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

	private static readonly HashSet<string> VersionlessKeys =
	[
		ApplicabilityKeys.Ess,
		ApplicabilityKeys.Ech,
		ApplicabilityKeys.Serverless,
		ApplicabilityKeys.Elasticsearch,
		ApplicabilityKeys.Observability,
		ApplicabilityKeys.Security,
		ApplicabilityKeys.VectorDb
	];

	private static bool TryGetApplicabilityOverTime(
		Dictionary<object, object?> dictionary,
		string key,
		List<ApplicabilityDiagnostic> diagnostics,
		out AppliesCollection? availability
	)
	{
		availability = null;
		if (!dictionary.TryGetValue(key, out var target))
			return false;

		if (target is null || (target is string s && string.IsNullOrWhiteSpace(s)))
			availability = AppliesCollection.GenerallyAvailable;
		else if (target is string stackString)
		{
			availability = ParseCollection(stackString, diagnostics, out var a) ? a : null;

			if (availability is not null)
				ValidateApplicabilityCollection(key, availability, diagnostics);
		}
		return availability is not null;
	}

	private static void ValidateApplicabilityCollection(string key, AppliesCollection collection, List<ApplicabilityDiagnostic> diagnostics)
	{
		var items = collection.ToList();

		// Rule: Versionless products cannot have version specifications
		if (VersionlessKeys.Contains(key))
		{
			if (items.Any(a => a.Version is not null && a.Version != AllVersionsSpec.Instance))
				diagnostics.Add(
					new ApplicabilityDiagnostic(
						Severity.Error,
						$"Can't specify a version for '{key}' because this product is not versioned. Remove the version, or use 'stack:' for version-specific requirements.",
						ApplicableToConstraints.VersionlessKeysHaveNoVersion.Name
					)
				);
			return;
		}

		// Rule: Only one version declaration per lifecycle
		var lifecycleGroups = items.GroupBy(a => a.Lifecycle).ToList();
		var lifecyclesWithMultipleVersions = lifecycleGroups
			.Where(group => group.Count(a => a.Version is not null && a.Version != AllVersionsSpec.Instance) > 1)
			.Select(g => g.Key)
			.ToList();

		if (lifecyclesWithMultipleVersions.Count > 0)
		{
			var lifecycleNames = string.Join(", ", lifecyclesWithMultipleVersions);
			diagnostics.Add(
				new ApplicabilityDiagnostic(
					Severity.Hint,
					$"Key '{key}': Multiple version declarations found for lifecycle(s): {lifecycleNames}. Only one version per lifecycle is allowed.",
					ApplicableToConstraints.ApplicabilityVersionsConsistent.Name
				) // Temporary downgrade to Hint until the currently available docs are adjusted

			);
		}

		// Rule: Only one item per key can use greater-than syntax
		var greaterThanItems = items.Where(
			a => a.Version is { Kind: VersionSpecKind.GreaterThanOrEqual } && a.Version != AllVersionsSpec.Instance
		).ToList();

		if (greaterThanItems.Count > 1)
		{
			diagnostics.Add(
				new ApplicabilityDiagnostic(
					Severity.Hint,
					$"Key '{key}': Multiple items use greater-than-or-equal syntax. Only one item per key can use this syntax.",
					ApplicableToConstraints.ApplicabilityVersionsConsistent.Name
				) // Temporary downgrade to Hint until the currently available docs are adjusted

			);
		}

		// Rule: In a range, the first version must be less than or equal the last version
		var invalidRanges = items.Where(
			a => a.Version is { Kind: VersionSpecKind.Range } && a.Version!.Min.CompareTo(a.Version.Max!) > 0
		).ToList();

		if (invalidRanges.Count > 0)
		{
			var rangeDescriptions = invalidRanges.Select(
				item =>
					$"{item.Lifecycle} ({item.Version!.Min.Major}.{item.Version.Min.Minor}-{item.Version.Max!.Major}.{item.Version.Max.Minor})"
			);
			diagnostics.Add(
				new ApplicabilityDiagnostic(
					Severity.Hint,
					$"Key '{key}': Invalid range(s) where first version is greater than last version: {string.Join(", ", rangeDescriptions)}.",
					ApplicableToConstraints.ApplicabilityVersionsConsistent.Name
				) // Temporary downgrade to Hint until the currently available docs are adjusted

			);
		}

		// Rule: No overlapping version ranges
		var versionedItems = items.Where(a => a.Version is not null && a.Version != AllVersionsSpec.Instance).ToList();

		var hasOverlaps = false;
		for (var i = 0; i < versionedItems.Count && !hasOverlaps; i++)
		{
			for (var j = i + 1; j < versionedItems.Count && !hasOverlaps; j++)
			{
				if (CheckVersionOverlap(versionedItems[i].Version!, versionedItems[j].Version!))
					hasOverlaps = true;
			}
		}

		if (hasOverlaps)
		{
			diagnostics.Add(
				new ApplicabilityDiagnostic(
					Severity.Hint,
					$"Key '{key}': Overlapping version ranges detected. Ensure version ranges do not overlap within the same key.",
					ApplicableToConstraints.ApplicabilityVersionsConsistent.Name
				) // Temporary downgrade to Hint until the currently available docs are adjusted

			);
		}
	}

	private static bool CheckVersionOverlap(VersionSpec v1, VersionSpec v2)
	{
		// Allow overlap in case there is a version bump
		if (
			v1.Kind == VersionSpecKind.Range
			&& v2.Kind == VersionSpecKind.GreaterThanOrEqual
			&& v1.Max is not null
			&& v1.Max.CompareTo(v2.Min) <= 0
		)
			return false;
		if (
			v2.Kind == VersionSpecKind.Range
			&& v1.Kind == VersionSpecKind.GreaterThanOrEqual
			&& v2.Max is not null
			&& v2.Max.CompareTo(v1.Min) <= 0
		)
			return false;

		// Get the effective ranges for each version spec
		// For GreaterThanOrEqual: [min, infinity)
		// For Range: [min, max]
		// For Exact: [exact, exact]

		var (v1Min, v1Max) = GetEffectiveRange(v1);
		var (v2Min, v2Max) = GetEffectiveRange(v2);

		return v1Min.CompareTo(v2Max ?? AllVersions.Instance) <= 0 && v2Min.CompareTo(v1Max ?? AllVersions.Instance) <= 0;
	}

	private static (SemVersion min, SemVersion? max) GetEffectiveRange(VersionSpec spec) => spec.Kind switch
	{
		VersionSpecKind.Exact => (spec.Min, spec.Min),
		VersionSpecKind.Range => (spec.Min, spec.Max),
		VersionSpecKind.GreaterThanOrEqual => (spec.Min, null),
		_ => throw new ArgumentOutOfRangeException(nameof(spec), spec.Kind, "Unknown VersionSpecKind")
	};

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) => serializer.Invoke(value, type);
}
