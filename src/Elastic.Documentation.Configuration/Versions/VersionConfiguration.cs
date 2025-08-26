// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Versions;

[YamlSerializable]
public record VersionsConfiguration
{
	public required IReadOnlyDictionary<string, Product> Products { get; init; }
	public required IReadOnlyDictionary<VersioningSystemId, VersioningSystem> VersioningSystems { get; init; }
	public VersioningSystem GetVersioningSystem(VersioningSystemId versioningSystem)
	{
		if (!VersioningSystems.TryGetValue(versioningSystem, out var version))
			throw new ArgumentException($"Unknown versioning system: {versioningSystem}");
		return version;
	}
}

[YamlSerializable]
public record Product
{
	public required string Id { get; init; }
	public required string DisplayName { get; init; }
	public required VersioningSystemId VersionSystem { get; init; }

	public static IReadOnlyCollection<Product> All(VersionsConfiguration versions) => [.. versions.Products.Values];
	public static IReadOnlyDictionary<string, Product> AllById(VersionsConfiguration versions) => versions.Products;
}

[EnumExtensions]
public enum VersioningSystemId
{
	[Display(Name = "stack")]
	Stack,
	[Display(Name = "all")]
	All,
	[Display(Name = "ece")]
	Ece,
	[Display(Name = "ech")]
	Ech,
	[Display(Name = "eck")]
	Eck,
	[Display(Name = "ess")]
	Ess,
	[Display(Name = "self")]
	Self,
	[Display(Name = "ecctl")]
	Ecctl,
	[Display(Name = "curator")]
	Curator,
	[Display(Name = "serverless")]
	Serverless,
	[Display(Name = "elasticsearch")]
	ElasticsearchProject,
	[Display(Name = "observability")]
	ObservabilityProject,
	[Display(Name = "security")]
	SecurityProject,
	[Display(Name = "apm_agent_android")]
	ApmAgentAndroid,
	[Display(Name = "apm_agent_ios")]
	ApmAgentIos,
	[Display(Name = "apm_agent_dotnet")]
	ApmAgentDotnet,
	[Display(Name = "apm_agent_go")]
	ApmAgentGo,
	[Display(Name = "apm_agent_java")]
	ApmAgentJava,
	[Display(Name = "apm_agent_node")]
	ApmAgentNode,
	[Display(Name = "apm_agent_php")]
	ApmAgentPhp,
	[Display(Name = "apm_agent_python")]
	ApmAgentPython,
	[Display(Name = "apm_agent_ruby")]
	ApmAgentRuby,
	[Display(Name = "apm_agent_rum")]
	ApmAgentRum,
	[Display(Name = "edot_ios")]
	EdotIos,
	[Display(Name = "edot_android")]
	EdotAndroid,
	[Display(Name = "edot_dotnet")]
	EdotDotnet,
	[Display(Name = "edot_java")]
	EdotJava,
	[Display(Name = "edot_node")]
	EdotNode,
	[Display(Name = "edot_php")]
	EdotPhp,
	[Display(Name = "edot_python")]
	EdotPython,
	[Display(Name = "edot_cf_aws")]
	EdotCfAws,
	[Display(Name = "edot_collector")]
	EdotCollector
}

[YamlSerializable]
public record VersioningSystem
{
	public required VersioningSystemId Id { get; init; }

	[YamlMember(Alias = "base")]
	public required SemVersion Base { get; init; }

	[YamlMember(Alias = "current")]
	public required SemVersion Current { get; init; }
}
