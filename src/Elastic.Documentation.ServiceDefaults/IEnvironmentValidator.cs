// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ServiceDefaults;

/// <summary>
/// Validates and normalizes an <c>ENVIRONMENT</c> value for a specific build type.
/// Returns the normalized value when valid, or a safe fallback when not.
/// </summary>
public interface IEnvironmentValidator
{
	/// <summary>
	/// Validates <paramref name="rawEnvironment"/> and returns the normalized environment name.
	/// Returns the fallback environment when <paramref name="rawEnvironment"/> is null, empty,
	/// or not in the allowed set for this build type.
	/// </summary>
	string Resolve(string? rawEnvironment);
}
