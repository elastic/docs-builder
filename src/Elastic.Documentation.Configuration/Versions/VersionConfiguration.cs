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
	public required IReadOnlyDictionary<VersioningSystemId, VersioningSystem> VersioningSystems { get; init; }
	public VersioningSystem GetVersioningSystem(VersioningSystemId versioningSystem)
	{
		if (!VersioningSystems.TryGetValue(versioningSystem, out var version))
			throw new ArgumentException($"Unknown versioning system: {versioningSystem}");
		return version;
	}
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
	[Display(Name = "apm_attacher")]
	ApmAttacher,
	[Display(Name = "apm_lambda")]
	ApmLambda,
	[Display(Name = "ecs_logging_dotnet")]
	EcsLoggingDotnet,
	[Display(Name = "ecs_logging_go_logrus")]
	EcsLoggingGoLogrus,
	[Display(Name = "ecs_logging_go_zap")]
	EcsLoggingGoZap,
	[Display(Name = "ecs_logging_go_zerolog")]
	EcsLoggingGoZerolog,
	[Display(Name = "ecs_logging_java")]
	EcsLoggingJava,
	[Display(Name = "ecs_logging_nodejs")]
	EcsLoggingNodeJs,
	[Display(Name = "ecs_logging_php")]
	EcsLoggingPhp,
	[Display(Name = "ecs_logging_python")]
	EcsLoggingPython,
	[Display(Name = "ecs_logging_ruby")]
	EcsLoggingRuby,
	[Display(Name = "esf")]
	Esf,
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
	EdotCollector,
	[Display(Name = "search_ui")]
	SearchUI
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
