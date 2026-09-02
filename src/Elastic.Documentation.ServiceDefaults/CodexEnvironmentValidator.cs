// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ServiceDefaults;

/// <summary>
/// Validates the deployment environment for codex builds.
/// Extend <see cref="Allowed"/> as new codex environments are introduced (e.g. <c>security</c>).
/// Falls back to <c>dev</c> for unrecognized values.
/// </summary>
internal sealed class CodexEnvironmentValidator : IEnvironmentValidator
{
	// Extend as new codex environments are added.
	private static readonly string[] Allowed = ["internal"];

	public string Resolve(string? rawEnvironment)
	{
		var env = rawEnvironment?.Trim().ToLowerInvariant();
		return env is not null && Allowed.Contains(env) ? env : "dev";
	}
}
