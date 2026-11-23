// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.FileSystems;
using Nullean.Snapshots;

namespace Elastic.Documentation.Build.Tests.Snapshots;

/// <summary>
/// How a feature flag resolves from the YAML value, the FEATURE_* environment variable and an explicit setter, as tables.
/// Shares the lock of <see cref="FeatureFlagsTests"/>: both change process-wide environment variables.
/// </summary>
[NotInParallel(nameof(FeatureFlagsTests))]
public class FeatureFlagsMatrixTests
{
	private static readonly string?[] EnvironmentValues = [null, "true", "false", "TRUE", "0", "1", "garbage"];
	private static readonly bool?[] YamlValues = [null, true, false];

	private static string EnvName(string key) => $"FEATURE_{key.ToUpperInvariant().Replace('-', '_')}";

	private static T WithCleanEnvironment<T>(Func<T> action)
	{
		var saved = FeatureFlags.KnownKeys.ToDictionary(k => k, k => Environment.GetEnvironmentVariable(EnvName(k)));
		try
		{
			foreach (var key in FeatureFlags.KnownKeys)
				Environment.SetEnvironmentVariable(EnvName(key), null);
			return action();
		}
		finally
		{
			foreach (var (key, value) in saved)
				Environment.SetEnvironmentVariable(EnvName(key), value);
		}
	}

	private static string Cell(bool?[] yamlValues, string?[] envValues, Func<bool?, string?, bool> resolve)
	{
		var sb = new StringBuilder();
		foreach (var yaml in yamlValues)
		{
			foreach (var env in envValues)
				_ = sb.Append(
					CultureInfo.InvariantCulture,
					$"yaml={yaml?.ToString().ToLowerInvariant() ?? "absent"} env={env ?? "unset"} => {resolve(yaml, env)}\n"
				);
		}
		return sb.ToString();
	}

	[Test]
	public void ResolutionOfOneFlag_YamlTimesEnvironmentVariable() =>
		WithCleanEnvironment(() => Cell(YamlValues, EnvironmentValues, (yaml, env) =>
		{
			Environment.SetEnvironmentVariable(EnvName("page-feedback"), env);
			var flags = new FeatureFlags(yaml is { } y ? new Dictionary<string, bool> { ["page-feedback"] = y } : []);
			return flags.PageFeedbackEnabled;
		})).Should().MatchSnapshot();

	[Test]
	public void ResolutionOfEveryKnownFlag_WithOnlyYamlTrue_ThenWithEnvironmentVariableFalse()
	{
		var table = WithCleanEnvironment(() =>
		{
			var sb = new StringBuilder("key | yaml only | yaml true + env false | no yaml + env true | explicit setter true + env false\n");
			foreach (var key in FeatureFlags.KnownKeys)
			{
				var yaml = new Dictionary<string, bool> { [key] = true };
				var yamlOnly = new FeatureFlags(yaml).EnabledKeys().Contains(key);

				Environment.SetEnvironmentVariable(EnvName(key), "false");
				var yamlEnvFalse = new FeatureFlags(yaml).EnabledKeys().Contains(key);
				var explicitSet = new FeatureFlags([]);
				explicitSet.Set(key, true);
				var setterEnvFalse = explicitSet.EnabledKeys().Contains(key);

				Environment.SetEnvironmentVariable(EnvName(key), "true");
				var envTrue = new FeatureFlags([]).EnabledKeys().Contains(key);
				Environment.SetEnvironmentVariable(EnvName(key), null);

				_ = sb.Append(CultureInfo.InvariantCulture, $"{key} | {yamlOnly} | {yamlEnvFalse} | {envTrue} | {setterEnvFalse}\n");
			}
			return sb.ToString();
		});
		table.Should().MatchSnapshot();
	}

	[Test]
	public void SetNormalisesKeys_AndTheTypedPropertiesWriteTheirOwnKey() =>
		WithCleanEnvironment(() =>
		{
			var sb = new StringBuilder();
			string[] keys =
			[
				"PAGE_FEEDBACK",
				"page_feedback",
				"Page-Feedback",
				"page-feedback",
				" page-feedback",
				"PAGE.FEEDBACK",
				"unknown-flag",
				"UNKNOWN_FLAG"
			];
			foreach (var key in keys)
			{
				var flags = new FeatureFlags([]);
				flags.Set(key, true);
				_ = sb.Append(
					CultureInfo.InvariantCulture,
					$"Set('{key}', true) => PageFeedbackEnabled={flags.PageFeedbackEnabled} enabled=[{string.Join(',', flags.EnabledKeys())}]\n"
				);
			}

			var constructed = new FeatureFlags(new Dictionary<string, bool> { ["PAGE_FEEDBACK"] = true, ["page-feedback"] = false });
			_ = sb.Append(
				CultureInfo.InvariantCulture,
				$"constructor does not normalise: PAGE_FEEDBACK=true => PageFeedbackEnabled={constructed.PageFeedbackEnabled}\n"
			);

			foreach (var property in typeof(FeatureFlags)
				.GetProperties()
				.Where(p => p.PropertyType == typeof(bool) && p.CanWrite)
				.OrderBy(p => p.Name, StringComparer.Ordinal))
			{
				var flags = new FeatureFlags([]);
				property.SetValue(flags, true);
				var afterTrue = string.Join(',', flags.EnabledKeys());
				property.SetValue(flags, false);
				_ = sb.Append(
					CultureInfo.InvariantCulture,
					$"{property.Name}: setter true => [{afterTrue}], then false => [{string.Join(',', flags.EnabledKeys())}]\n"
				);
			}
			return sb.ToString();
		}).Should().MatchSnapshot();

	[Test]
	public void AssemblerEnvironments_EnabledKeysWithoutAndWithOverrides()
	{
		var config = AssemblyConfiguration.Create(new ConfigurationFileProvider(new TestLoggerFactory(), new ConfigurationFileSystem()));
		var table = WithCleanEnvironment(() =>
		{
			var sb = new StringBuilder();
			foreach (var (name, environment) in config.Environments.OrderBy(e => e.Key, StringComparer.Ordinal))
			{
				var raw = string.Join(
					", ",
					environment
						.FeatureFlags
						.OrderBy(f => f.Key, StringComparer.Ordinal)
						.Select(f => $"{f.Key}={f.Value.ToString().ToLowerInvariant()}")
				);
				_ = sb.Append(
					CultureInfo.InvariantCulture,
					$"{name}\n  yaml:    {raw}\n  enabled: {string.Join(", ", environment.ToFeatureFlags().EnabledKeys())}\n"
				);

				Environment.SetEnvironmentVariable(EnvName("page-feedback"), "false");
				Environment.SetEnvironmentVariable(EnvName("guide-nav"), "true");
				_ = sb.Append(
					CultureInfo.InvariantCulture,
					$"  enabled with FEATURE_PAGE_FEEDBACK=false and FEATURE_GUIDE_NAV=true: {string.Join(", ", environment.ToFeatureFlags().EnabledKeys())}\n"
				);
				Environment.SetEnvironmentVariable(EnvName("page-feedback"), null);
				Environment.SetEnvironmentVariable(EnvName("guide-nav"), null);
			}
			return sb.ToString();
		});
		table.Should().MatchSnapshot();
	}
}
