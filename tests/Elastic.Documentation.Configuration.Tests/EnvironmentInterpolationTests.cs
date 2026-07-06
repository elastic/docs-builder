// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class EnvironmentInterpolationTests
{
	private const string AllowedVar = "KIBANA_STORYBOOK_REGISTRY";
	private const string DefaultRegistry = "https://ci-artifacts.kibana.dev/storybooks/main/storybook-docs/docs_registry.json";

	[Fact]
	public void DefaultIsUsed_WhenAllowedVariableUnset()
	{
		var environment = new MockEnvironment();

		var result = EnvironmentInterpolation.Interpolate($"${{{AllowedVar}:-{DefaultRegistry}}}", environment);

		result.Value.Should().Be(DefaultRegistry);
		result.Fallback.Should().BeNull();
	}

	[Fact]
	public void EnvironmentValueIsUsed_WhenAllowedVariableSet()
	{
		const string prRegistry = "https://ci-artifacts.kibana.dev/storybooks/pr-42/storybook-docs/docs_registry.json";
		var environment = new MockEnvironment { [AllowedVar] = prRegistry };

		var result = EnvironmentInterpolation.Interpolate($"${{{AllowedVar}:-{DefaultRegistry}}}", environment);

		result.Value.Should().Be(prRegistry);
		result.Fallback.Should().Be(DefaultRegistry);
	}

	[Fact]
	public void EmptyEnvironmentValue_FallsBackToDefault()
	{
		var environment = new MockEnvironment { [AllowedVar] = "" };

		var result = EnvironmentInterpolation.Interpolate($"${{{AllowedVar}:-{DefaultRegistry}}}", environment);

		result.Value.Should().Be(DefaultRegistry);
		result.Fallback.Should().BeNull();
	}

	[Fact]
	public void DisallowedVariable_IsNotReadFromEnvironment_AndLeftLiteral()
	{
		var environment = new MockEnvironment { ["AWS_SECRET_ACCESS_KEY"] = "super-secret" };
		string? reported = null;

		var result = EnvironmentInterpolation.Interpolate("${AWS_SECRET_ACCESS_KEY}", environment, name => reported = name);

		result.Value.Should().Be("${AWS_SECRET_ACCESS_KEY}");
		result.Value.Should().NotContain("super-secret");
		result.Fallback.Should().BeNull();
		reported.Should().Be("AWS_SECRET_ACCESS_KEY");
	}

	[Fact]
	public void DisallowedVariableWithDefault_DoesNotResolveToDefault()
	{
		var environment = new MockEnvironment();

		var result = EnvironmentInterpolation.Interpolate("${SECRET:-fallback}", environment);

		result.Value.Should().Be("${SECRET:-fallback}");
	}

	[Fact]
	public void AllowedVariableWithoutDefault_Unset_ResolvesToEmpty()
	{
		var environment = new MockEnvironment();

		var result = EnvironmentInterpolation.Interpolate($"prefix-${{{AllowedVar}}}-suffix", environment);

		result.Value.Should().Be("prefix--suffix");
	}

	[Fact]
	public void NoExpression_ReturnsRawUnchanged()
	{
		var environment = new MockEnvironment { [AllowedVar] = "ignored" };

		var result = EnvironmentInterpolation.Interpolate(DefaultRegistry, environment);

		result.Value.Should().Be(DefaultRegistry);
		result.Fallback.Should().BeNull();
	}

	[Fact]
	public void NullInput_ReturnsNull()
	{
		var result = EnvironmentInterpolation.Interpolate(null, new MockEnvironment());

		result.Value.Should().BeNull();
		result.Fallback.Should().BeNull();
	}

	private sealed class MockEnvironment : IEnvironmentVariables
	{
		private readonly Dictionary<string, string?> _variables = [with(StringComparer.Ordinal)];

		public string? this[string name]
		{
			set => _variables[name] = value;
		}

		public string? GetEnvironmentVariable(string name) =>
			_variables.GetValueOrDefault(name);

		public bool IsRunningOnCI => false;
	}
}
