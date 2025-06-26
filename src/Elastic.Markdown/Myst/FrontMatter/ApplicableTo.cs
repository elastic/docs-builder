// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

/// Use to collect diagnostics during yaml parsing where we do not have access to the current diagnostics collector
public class YamlDiagnosticsCollection : IEquatable<YamlDiagnosticsCollection>, IReadOnlyCollection<(Severity, string)>
{
	private readonly List<(Severity, string)> _list = [];

	public YamlDiagnosticsCollection(IEnumerable<(Severity, string)> warnings) => _list.AddRange(warnings);

	public bool Equals(YamlDiagnosticsCollection? other) => other != null && _list.SequenceEqual(other._list);

	public IEnumerator<(Severity, string)> GetEnumerator() => _list.GetEnumerator();

	public override bool Equals(object? obj) => Equals(obj as YamlDiagnosticsCollection);

	public override int GetHashCode() => _list.GetHashCode();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public int Count => _list.Count;
}

public interface IApplicableToElement
{
	ApplicableTo? AppliesTo { get; }
}

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

	public ProductApplicability? ProductApplicability { get; set; }

	internal YamlDiagnosticsCollection? Diagnostics { get; set; }

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

	[YamlMember(Alias = "ech")]
	public AppliesCollection? Ech { get; set; }

	public static DeploymentApplicability All { get; } = new()
	{
		Ece = AppliesCollection.GenerallyAvailable,
		Eck = AppliesCollection.GenerallyAvailable,
		Ess = AppliesCollection.GenerallyAvailable,
		Ech = AppliesCollection.GenerallyAvailable,
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

[YamlSerializable]
public record ProductApplicability
{
	[YamlMember(Alias = "ecctl")]
	public AppliesCollection? Ecctl { get; set; }

	[YamlMember(Alias = "curator")]
	public AppliesCollection? Curator { get; set; }

	[YamlMember(Alias = "apm_agent_dotnet")]
	public AppliesCollection? ApmAgentDotnet { get; set; }

	[YamlMember(Alias = "apm_agent_go")]
	public AppliesCollection? ApmAgentGo { get; set; }

	[YamlMember(Alias = "apm_agent_java")]
	public AppliesCollection? ApmAgentJava { get; set; }

	[YamlMember(Alias = "apm_agent_node")]
	public AppliesCollection? ApmAgentNode { get; set; }

	[YamlMember(Alias = "apm_agent_python")]
	public AppliesCollection? ApmAgentPython { get; set; }

	[YamlMember(Alias = "apm_agent_ruby")]
	public AppliesCollection? ApmAgentRuby { get; set; }

	[YamlMember(Alias = "apm_agent_rum")]
	public AppliesCollection? ApmAgentRum { get; set; }

	[YamlMember(Alias = "edot_ios")]
	public AppliesCollection? EdotIos { get; set; }

	[YamlMember(Alias = "edot_android")]
	public AppliesCollection? EdotAndroid { get; set; }

	[YamlMember(Alias = "edot_dotnet")]
	public AppliesCollection? EdotDotnet { get; set; }

	[YamlMember(Alias = "edot_java")]
	public AppliesCollection? EdotJava { get; set; }

	[YamlMember(Alias = "edot_node")]
	public AppliesCollection? EdotNode { get; set; }

	[YamlMember(Alias = "edot_php")]
	public AppliesCollection? EdotPhp { get; set; }

	[YamlMember(Alias = "edot_python")]
	public AppliesCollection? EdotPython { get; set; }
}

public class ApplicableToConverter : IYamlTypeConverter
{
	private static readonly string[] KnownKeys =
	[
		"stack", "deployment", "serverless", "product",
		"ece", "eck", "ess", "self",
		"elasticsearch", "observability", "security",
		"ecctl", "curator",
		"apm_agent_dotnet", "apm_agent_go", "apm_agent_java", "apm_agent_node", "apm_agent_python", "apm_agent_ruby", "apm_agent_rum",
		"edot_ios", "edot_android", "edot_dotnet", "edot_java", "edot_node", "edot_php", "edot_python"
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

			if (string.Equals(value.Value, "all", StringComparison.InvariantCultureIgnoreCase))
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


		if (keys.Contains("ess"))
		{
			diagnostics.Add(keys.Contains("ech")
				? (Severity.Warning, "Cannot use both 'ech' and 'ess'. 'ess' is deprecated, please use 'ech' instead.")
				: (Severity.Hint, "The 'ess' key is deprecated, please use 'ech' instead."));
		}

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
			applicableTo.Diagnostics = new YamlDiagnosticsCollection(diagnostics);
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
				Ech = av,
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
		if (TryGetApplicabilityOverTime(dictionary, "ece", diagnostics, out var ece))
		{
			d.Ece = ece;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "eck", diagnostics, out var eck))
		{
			d.Eck = eck;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "ess", diagnostics, out var ess))
		{
			d.Ess = ess;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "self", diagnostics, out var self))
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

	private static bool TryGetProjectApplicability(Dictionary<object, object?> dictionary,
		List<(Severity, string)> diagnostics,
		[NotNullWhen(true)] out ServerlessProjectApplicability? applicability)
	{
		applicability = null;
		var serverlessAvailability = new ServerlessProjectApplicability();
		var assigned = false;
		if (TryGetApplicabilityOverTime(dictionary, "elasticsearch", diagnostics, out var elasticsearch))
		{
			serverlessAvailability.Elasticsearch = elasticsearch;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "observability", diagnostics, out var observability))
		{
			serverlessAvailability.Observability = observability;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "security", diagnostics, out var security))
		{
			serverlessAvailability.Security = security;
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
		if (TryGetApplicabilityOverTime(dictionary, "ecctl", diagnostics, out var ecctl))
		{
			productAvailability.Ecctl = ecctl;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "curator", diagnostics, out var curator))
		{
			productAvailability.Curator = curator;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "apm_agent_dotnet", diagnostics, out var apmAgentDotnet))
		{
			productAvailability.ApmAgentDotnet = apmAgentDotnet;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "apm_agent_go", diagnostics, out var apmAgentGo))
		{
			productAvailability.ApmAgentGo = apmAgentGo;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "apm_agent_java", diagnostics, out var apmAgentJava))
		{
			productAvailability.ApmAgentJava = apmAgentJava;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "apm_agent_node", diagnostics, out var apmAgentNode))
		{
			productAvailability.ApmAgentNode = apmAgentNode;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "apm_agent_python", diagnostics, out var apmAgentPython))
		{
			productAvailability.ApmAgentPython = apmAgentPython;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "apm_agent_ruby", diagnostics, out var apmAgentRuby))
		{
			productAvailability.ApmAgentRuby = apmAgentRuby;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "apm_agent_rum", diagnostics, out var apmAgentRum))
		{
			productAvailability.ApmAgentRum = apmAgentRum;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "edot_ios", diagnostics, out var edotIos))
		{
			productAvailability.EdotIos = edotIos;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "edot_android", diagnostics, out var edotAndroid))
		{
			productAvailability.EdotAndroid = edotAndroid;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "edot_dotnet", diagnostics, out var edotDotnet))
		{
			productAvailability.EdotDotnet = edotDotnet;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "edot_java", diagnostics, out var edotJava))
		{
			productAvailability.EdotJava = edotJava;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "edot_node", diagnostics, out var edotNode))
		{
			productAvailability.EdotNode = edotNode;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "edot_php", diagnostics, out var edotPhp))
		{
			productAvailability.EdotPhp = edotPhp;
			assigned = true;
		}

		if (TryGetApplicabilityOverTime(dictionary, "edot_python", diagnostics, out var edotPython))
		{
			productAvailability.EdotPython = edotPython;
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
