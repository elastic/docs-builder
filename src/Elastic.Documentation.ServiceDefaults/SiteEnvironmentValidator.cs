// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ServiceDefaults;

/// <summary>
/// Validates the deployment environment for the main documentation site.
/// Accepts <c>dev</c>, <c>edge</c>, <c>staging</c>, and <c>prod</c>; falls back to <c>dev</c>.
/// </summary>
internal sealed class SiteEnvironmentValidator : IEnvironmentValidator
{
	private static readonly string[] Allowed = ["dev", "edge", "staging", "prod"];

	public string Resolve(string? rawEnvironment)
	{
		var env = rawEnvironment?.Trim().ToLowerInvariant();
		return env is not null && Allowed.Contains(env) ? env : "dev";
	}
}
