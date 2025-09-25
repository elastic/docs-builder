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
	[Display(Name = "ecs")]
	Ecs,
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
	[Display(Name = "apm-agent-android")]
	ApmAgentAndroid,
	[Display(Name = "apm-agent-ios")]
	ApmAgentIos,
	[Display(Name = "apm-agent-dotnet")]
	ApmAgentDotnet,
	[Display(Name = "apm-agent-go")]
	ApmAgentGo,
	[Display(Name = "apm-agent-java")]
	ApmAgentJava,
	[Display(Name = "apm-agent-node")]
	ApmAgentNode,
	[Display(Name = "apm-agent-php")]
	ApmAgentPhp,
	[Display(Name = "apm-agent-python")]
	ApmAgentPython,
	[Display(Name = "apm-agent-ruby")]
	ApmAgentRuby,
	[Display(Name = "apm-agent-rum-js")]
	ApmAgentRumJs,
	[Display(Name = "apm-attacher")]
	ApmAttacher,
	[Display(Name = "apm-lambda")]
	ApmLambda,
	[Display(Name = "ecs-logging-dotnet")]
	EcsLoggingDotnet,
	[Display(Name = "ecs-logging-go-logrus")]
	EcsLoggingGoLogrus,
	[Display(Name = "ecs-logging-go-zap")]
	EcsLoggingGoZap,
	[Display(Name = "ecs-logging-go-zerolog")]
	EcsLoggingGoZerolog,
	[Display(Name = "ecs-logging-java")]
	EcsLoggingJava,
	[Display(Name = "ecs-logging-nodejs")]
	EcsLoggingNodeJs,
	[Display(Name = "ecs-logging-php")]
	EcsLoggingPhp,
	[Display(Name = "ecs-logging-python")]
	EcsLoggingPython,
	[Display(Name = "ecs-logging-ruby")]
	EcsLoggingRuby,
	[Display(Name = "esf")]
	Esf,
	[Display(Name = "edot-ios")]
	EdotIos,
	[Display(Name = "edot-android")]
	EdotAndroid,
	[Display(Name = "edot-dotnet")]
	EdotDotnet,
	[Display(Name = "edot-java")]
	EdotJava,
	[Display(Name = "edot-node")]
	EdotNode,
	[Display(Name = "edot-php")]
	EdotPhp,
	[Display(Name = "edot-python")]
	EdotPython,
	[Display(Name = "edot-cf-aws")]
	EdotCfAws,
	[Display(Name = "edot-cf-azure")]
	EdotCfAzure,
	[Display(Name = "edot-collector")]
	EdotCollector,
	[Display(Name = "search-ui")]
	SearchUI,
	[Display(Name = "cloud-terraform")]
	CloudTerraform,
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
