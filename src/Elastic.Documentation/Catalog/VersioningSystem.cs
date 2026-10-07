// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using Elastic.Documentation.Versions;
using Generator.Equals;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation.Catalog;

/// <summary>Every versioning system from <c>versions.yml</c>, keyed by id. Part of the <see cref="GlobalCatalog"/> aggregate.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>CatalogLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, which reads <c>versions.yml</c>, <c>products.yml</c> and <c>legacy-url-mappings.yml</c> as one unit and returns a <c>Result&lt;GlobalCatalog&gt;</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>VersioningSystems</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="VersioningSystemConstraints"/>) reports it: <see cref="VersioningSystemConstraints.VersioningSystemsComplete"/> reports a versioning system id that has no entry.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new VersioningSystemCatalog { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public sealed partial record VersioningSystemCatalog
{
	[UnorderedEquality]
	public IReadOnlyDictionary<VersioningSystemId, VersioningSystem> VersioningSystems { get; internal init; } = default!;
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
	[Display(Name = "edot-browser")]
	EdotBrowser,
	[Display(Name = "edot-php")]
	EdotPhp,
	[Display(Name = "edot-python")]
	EdotPython,
	[Display(Name = "edot-cf-aws")]
	EdotCfAws,
	[Display(Name = "edot-cf-azure")]
	EdotCfAzure,
	[Display(Name = "edot-cf-gcp")]
	EdotCfGcp,
	[Display(Name = "terraform-google-edot-cf")]
	TerraformGoogleEdotCf,
	[Display(Name = "edot-collector")]
	EdotCollector,
	[Display(Name = "search-ui")]
	SearchUI,
	[Display(Name = "cloud-terraform")]
	CloudTerraform,
	[Display(Name = "elasticsearch-client-go")]
	ElasticsearchClientGo,
	[Display(Name = "elasticsearch-client-java")]
	ElasticsearchClientJava,
	[Display(Name = "elasticsearch-client-javascript")]
	ElasticsearchClientJavascript,
	[Display(Name = "elasticsearch-client-dotnet")]
	ElasticsearchClientDotnet,
	[Display(Name = "elasticsearch-client-php")]
	ElasticsearchClientPhp,
	[Display(Name = "elasticsearch-client-python")]
	ElasticsearchClientPython,
	[Display(Name = "elasticsearch-client-ruby")]
	ElasticsearchClientRuby,
	[Display(Name = "elasticsearch-client-rust")]
	ElasticsearchClientRust,

	[Display(Name = "none")]
	None = -1,
}

/// <summary>A version line with a <c>base</c> (first version the docs cover) and a <c>current</c> (latest release).</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>CatalogLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, which reads <c>versions.yml</c>, <c>products.yml</c> and <c>legacy-url-mappings.yml</c> as one unit and returns a <c>Result&lt;GlobalCatalog&gt;</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Id</c>, <c>Base</c>, <c>Current</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="VersioningSystemConstraints"/>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with <c>CatalogFixtures.Versioning</c> or <c>DomainGen.DefaultVersioning</c> (tests), or an object initializer (<c>new VersioningSystem { Id = ..., Base = ..., Current = ... }</c>), and <c>with</c> for variations.</para>
/// </remarks>
public sealed record VersioningSystem
{
	/// <summary>
	/// The sentinel major version value used for "versionless" products (serverless, cloud, etc.)
	/// Products with this version should not display a version dropdown.
	/// </summary>
	public const int VersionlessSentinel = 99999;

	/// <summary>
	/// A versionless sentinel for products that have no versioning system at all
	/// (e.g. release-notes-only products without public documentation).
	/// </summary>
	public static VersioningSystem None { get; } = new()
	{
		Id = VersioningSystemId.None,
		Base = new SemVersion(VersionlessSentinel, 0, 0),
		Current = new SemVersion(VersionlessSentinel, 0, 0)
	};

	public VersioningSystemId Id { get; internal init; } = default!;

	public SemVersion Base { get; internal init; } = default!;

	public SemVersion Current { get; internal init; } = default!;

	public bool IsVersioned() => Base.Major != AllVersions.Instance.Major;
	/// <summary>
	/// Returns true if this versioning system represents a "versionless" product
	/// (e.g., serverless, cloud services) that should not display a version dropdown.
	/// </summary>
	public bool IsVersionless => Current.Major >= VersionlessSentinel;
}
