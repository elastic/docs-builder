// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Configuration.Assembler;

public static class PublishEnvironmentExtensions
{
	/// <summary>
	/// Returns a normalized, env-var-overridable <see cref="FeatureFlags"/> view of the environment's flags.
	/// Use this rather than accessing the raw dictionary so that key normalization and env-var overrides
	/// are applied consistently with how docset-level flags are processed.
	/// </summary>
	public static FeatureFlags ToFeatureFlags(this PublishEnvironment environment)
	{
		var flags = new FeatureFlags([]);
		foreach (var (key, value) in environment.FeatureFlags)
			flags.Set(key, value);
		return flags;
	}
}
