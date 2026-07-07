// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation;

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Outcome of interpolating shell-style <c>${VAR}</c> / <c>${VAR:-default}</c> expressions into a config value.
/// </summary>
/// <param name="Value">The resolved value, using environment variables where set and defaults otherwise.</param>
/// <param name="Fallback">
/// The environment-independent value (every expression replaced by its committed default). Non-null only when an
/// allow-listed environment variable actually changed the result, so consumers can degrade to the committed default
/// if the environment-supplied value turns out to be unusable (e.g. an ephemeral PR registry that 404s).
/// </param>
public sealed record InterpolatedValue(string? Value, string? Fallback);

/// <summary>
/// Resolves shell-style <c>${VAR}</c> and <c>${VAR:-default}</c> expressions in committed config values.
/// docs-builder renders untrusted PR branches, so interpolation is restricted to an explicit allow-list — naive access
/// to the full process environment would let a malicious <c>docset.yml</c> exfiltrate CI secrets (e.g. <c>${AWS_SECRET_ACCESS_KEY}</c>).
/// </summary>
public static partial class EnvironmentInterpolation
{
	/// <summary>Environment variable names that may be interpolated into committed config values.</summary>
	public static readonly FrozenSet<string> AllowedVariables =
		new HashSet<string>(StringComparer.Ordinal) { "KIBANA_STORYBOOK_REGISTRY" }.ToFrozenSet(StringComparer.Ordinal);

	[GeneratedRegex(@"\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)(?::-(?<default>[^}]*))?\}", RegexOptions.CultureInvariant)]
	private static partial Regex ExpressionRegex();

	/// <summary>
	/// Interpolates allow-listed environment variables into <paramref name="raw"/>. Non-allow-listed expressions are
	/// left literal (never read from the environment) and reported via <paramref name="onDisallowed"/>.
	/// </summary>
	public static InterpolatedValue Interpolate(string? raw, IEnvironmentVariables environment, Action<string>? onDisallowed = null)
	{
		if (string.IsNullOrEmpty(raw) || !raw.Contains("${", StringComparison.Ordinal))
			return new InterpolatedValue(raw, null);

		var resolved = new StringBuilder(raw.Length);
		var committed = new StringBuilder(raw.Length);
		var lastIndex = 0;
		var environmentChangedValue = false;

		foreach (Match match in ExpressionRegex().Matches(raw))
		{
			var literal = raw[lastIndex..match.Index];
			_ = resolved.Append(literal);
			_ = committed.Append(literal);
			lastIndex = match.Index + match.Length;

			var name = match.Groups["name"].Value;
			var defaultGroup = match.Groups["default"];
			var defaultValue = defaultGroup.Success ? defaultGroup.Value : string.Empty;

			if (!AllowedVariables.Contains(name))
			{
				onDisallowed?.Invoke(name);
				_ = resolved.Append(match.Value);
				_ = committed.Append(match.Value);
				continue;
			}

			var environmentValue = environment.GetEnvironmentVariable(name);
			if (!string.IsNullOrEmpty(environmentValue))
			{
				_ = resolved.Append(environmentValue);
				environmentChangedValue = true;
			}
			else
				_ = resolved.Append(defaultValue);

			_ = committed.Append(defaultValue);
		}

		var tail = raw[lastIndex..];
		_ = resolved.Append(tail);
		_ = committed.Append(tail);

		var resolvedValue = resolved.ToString();
		var committedValue = committed.ToString();
		var fallback = environmentChangedValue && !string.Equals(resolvedValue, committedValue, StringComparison.Ordinal)
			? committedValue
			: null;

		return new InterpolatedValue(resolvedValue, fallback);
	}
}
