// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;
using Nullean.Entities;

namespace Elastic.Documentation.Codex;

/// <summary>
/// A <see cref="CodexConfiguration"/> that is about to be published: the stage after reading it. Publishing indexes into the codex's environment,
/// so the rules of this stage need one; reading the configuration (to update redirects, say) does not.
/// </summary>
[Equatable]
public sealed partial record PublishedCodexConfiguration(CodexConfiguration Configuration) : IHaveConstraints<PublishedCodexConfiguration>
{
	public static ConstraintSet<PublishedCodexConfiguration> Constraints => PublishedCodexConfigurationConstraints.All;
}
