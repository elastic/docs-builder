// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Codex;

/// <summary>
/// Every rule a <see cref="CodexConfiguration"/> must satisfy when it is read. The set is intentionally empty: it exists so that every loaded type
/// has a static <c>T.Constraints</c> (<c>Loader.For</c> requires it), and the loader accepts every value today.
/// The group id, group name, duplicate group id and canonical_base_url rules are deferred behaviour changes that will land here.
/// Adding a rule here changes what the codex commands report: it needs its own PR with a snapshot diff, not a refactor PR
/// (<c>CodexConfigurationConstraintsTests.ReadStage_IsIntentionallyEmpty</c> fails until that PR updates it).
/// </summary>
[Constraints<CodexConfiguration>]
public static partial class CodexConfigurationConstraints;

/// <summary>Every rule a codex must satisfy to be published, on top of being readable: the stage's own set, in the order they are reported.</summary>
[Constraints<PublishedCodexConfiguration>]
public static partial class PublishedCodexConfigurationConstraints
{
	public static readonly Constraint<PublishedCodexConfiguration> EnvironmentRequired = Constraints<PublishedCodexConfiguration>.Always(
		c => c.Configuration.Environment,
		e => !string.IsNullOrWhiteSpace(e),
		"Codex configuration must specify an 'environment' (e.g., 'internal', 'security')."
	).Describe("A codex that is published names its environment, which is its Elasticsearch index namespace.");
}
