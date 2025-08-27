// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Text.Json.Serialization;
using Elastic.Documentation.Diagnostics;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.AppliesTo;

/// Use to collect diagnostics during YAML parsing where we do not have access to the current diagnostics collector
public class ApplicabilityDiagnosticsCollection : IEquatable<ApplicabilityDiagnosticsCollection>, IReadOnlyCollection<(Severity, string)>
{
	private readonly List<(Severity, string)> _list = [];

	public ApplicabilityDiagnosticsCollection(IEnumerable<(Severity, string)> warnings) => _list.AddRange(warnings);

	public bool Equals(ApplicabilityDiagnosticsCollection? other) => other != null && _list.SequenceEqual(other._list);

	public IEnumerator<(Severity, string)> GetEnumerator() => _list.GetEnumerator();

	public override bool Equals(object? obj) => Equals(obj as ApplicabilityDiagnosticsCollection);

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

	[JsonIgnore]
	[YamlIgnore]
	public ApplicabilityDiagnosticsCollection? Diagnostics { get; set; }

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

[YamlSerializable]
public record ProductApplicability
{
	[YamlMember(Alias = "ecctl")]
	public AppliesCollection? Ecctl { get; set; }

	[YamlMember(Alias = "curator")]
	public AppliesCollection? Curator { get; set; }

	[YamlMember(Alias = "apm_agent_android")]
	public AppliesCollection? ApmAgentAndroid { get; set; }

	[YamlMember(Alias = "apm_agent_dotnet")]
	public AppliesCollection? ApmAgentDotnet { get; set; }

	[YamlMember(Alias = "apm_agent_go")]
	public AppliesCollection? ApmAgentGo { get; set; }

	[YamlMember(Alias = "apm_agent_ios")]
	public AppliesCollection? ApmAgentIos { get; set; }

	[YamlMember(Alias = "apm_agent_java")]
	public AppliesCollection? ApmAgentJava { get; set; }

	[YamlMember(Alias = "apm_agent_node")]
	public AppliesCollection? ApmAgentNode { get; set; }

	[YamlMember(Alias = "apm_agent_php")]
	public AppliesCollection? ApmAgentPhp { get; set; }

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

	[YamlMember(Alias = "edot_cf_aws")]
	public AppliesCollection? EdotCfAws { get; set; }

	[YamlMember(Alias = "edot_collector")]
	public AppliesCollection? EdotCollector { get; set; }
}
