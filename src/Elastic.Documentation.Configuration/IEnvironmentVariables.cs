// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Abstracts access to environment variables for testability.
/// Allows unit tests to mock environment variable values without modifying actual environment.
/// </summary>
public interface IEnvironmentVariables
{
	/// <summary>
	/// Gets the value of an environment variable.
	/// </summary>
	/// <param name="name">The name of the environment variable.</param>
	/// <returns>The value of the environment variable, or null if not set.</returns>
	string? GetEnvironmentVariable(string name);

	/// <summary>
	/// Indicates whether the current process is running in a CI environment.
	/// Checks for the presence of the GITHUB_ACTIONS environment variable.
	/// </summary>
	bool IsRunningOnCI { get; }
}
