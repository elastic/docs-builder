// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Builder;

// Declared in the domain namespace so the behavior arrives with the Publishing records it extends; it lives here because
// it needs loader-side dependencies. The same-named assembler.yml DTOs in this folder's namespace would otherwise shadow them.
#pragma warning disable IDE0130
namespace Elastic.Documentation.Publishing;

#pragma warning restore IDE0130

public static class PublishEnvironmentExtensions
{
	/// <summary>
	/// Returns a normalized, env-var-overridable <see cref="FeatureFlags"/> view of the environment's flags.
	/// Use this rather than accessing the raw dictionary so that key normalization and env-var overrides
	/// are applied consistently with how docset-level flags are processed.
	/// </summary>
	public static FeatureFlags ToFeatureFlags(this PublishEnvironment environment) => new FeatureFlags([]).With(environment.FeatureFlags);
}
