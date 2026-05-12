// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;

namespace Elastic.Documentation.Build.Tests;

/// <summary>
/// Mock implementation of <see cref="IEnvironmentVariables"/> for testing.
/// Allows tests to simulate different environment variable configurations
/// including CI/non-CI environments.
/// </summary>
public class MockEnvironmentVariables : IEnvironmentVariables
{
	private readonly Dictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Sets an environment variable value for testing.
	/// </summary>
	public void Set(string name, string value) => _variables[name] = value;

	/// <summary>
	/// Removes an environment variable for testing.
	/// </summary>
	public void Remove(string name) => _variables.Remove(name);

	/// <summary>
	/// Clears all environment variables.
	/// </summary>
	public void Clear() => _variables.Clear();

	/// <summary>
	/// Configures the mock to simulate a CI environment.
	/// </summary>
	public void SetCI(bool isCI)
	{
		if (isCI)
			_variables["GITHUB_ACTIONS"] = "true";
		else
			_variables.Remove("GITHUB_ACTIONS");
	}

	/// <inheritdoc />
	public string? GetEnvironmentVariable(string name) =>
		_variables.TryGetValue(name, out var value) ? value : null;

	/// <inheritdoc />
	public bool IsRunningOnCI =>
		!string.IsNullOrEmpty(GetEnvironmentVariable("GITHUB_ACTIONS"));

	/// <summary>
	/// Creates a mock environment that simulates running on CI.
	/// </summary>
	public static MockEnvironmentVariables CreateCI()
	{
		var mock = new MockEnvironmentVariables();
		mock.SetCI(true);
		return mock;
	}

	/// <summary>
	/// Creates a mock environment that simulates local development (not on CI).
	/// </summary>
	public static MockEnvironmentVariables CreateLocal() => new();
}
