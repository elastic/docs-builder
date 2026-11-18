// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Codex;

/// <summary>
/// Every rule a <see cref="CodexConfiguration"/> must satisfy when it is read. The loader accepts every value today, so there is none yet:
/// the group id and name, duplicate group id and canonical_base_url rules of the model are deferred behaviour changes.
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
