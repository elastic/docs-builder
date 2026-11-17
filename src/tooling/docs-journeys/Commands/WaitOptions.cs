// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Nullean.Cdp;
using Nullean.Cdp.Journeys;

namespace Elastic.Documentation.Journeys.Cli.Commands;

/// <summary>
/// What the site must show before a run starts: <c>--wait-for</c> checks in the syntax of <c>snap journey run</c>, and <c>--build</c>,
/// which waits for the landing page to name the docs-builder version a deploy shipped.
/// </summary>
internal static class WaitOptions
{
	/// <summary>The tag the landing page of an assembler build carries: the docs-builder version that rendered it.</summary>
	private const string BuildSelector = "meta[name='docs:build']";

	private static readonly (string Token, TextMatch Match)[] Operators =
	[
		("==", TextMatch.Exact),
		("=~", TextMatch.Regex),
		("=*", TextMatch.Contains)
	];

	/// <summary>
	/// Reads the checks: each <c>--wait-for</c> as <c>header:Name==value</c>, <c>selector==value</c> (text) or
	/// <c>selector@attribute==value</c>, with <c>=~</c> for a regular expression and <c>=*</c> for contains. These never occur in a CSS
	/// selector, so it needs no escaping. <c>--build</c> adds a check on the <c>docs:build</c> tag. A problem is a usage error.
	/// </summary>
	public static bool TryParse(
		string? build,
		IReadOnlyList<string>? waitFor,
		string? path,
		string? timeout,
		string? interval,
		int? retries,
		out List<JourneyPrecondition> preconditions,
		out string problem
	)
	{
		problem = string.Empty;
		preconditions = [];
		var checks = (waitFor ?? []).ToList();
		if (build is not null)
			checks.Add($"{BuildSelector}@content=={build}");
		if (checks.Count == 0)
		{
			if (path is not null || timeout is not null || interval is not null || retries is not null)
			{
				problem = "--wait-path, --wait-timeout, --wait-interval and --wait-retries describe a --wait-for or a --build; give one.";
				return false;
			}

			return true;
		}

		if (
			!TryDuration("--wait-timeout", timeout, TimeSpan.FromSeconds(10), out var wait, out problem)
			|| !TryDuration("--wait-interval", interval, TimeSpan.FromMilliseconds(500), out var every, out problem)
		)
			return false;
		if (retries is < 0)
		{
			problem = $"--wait-retries {retries} is not zero or more.";
			return false;
		}

		foreach (var check in checks)
		{
			if (!TryCheck(check, out var parsed, out problem))
				return false;
			preconditions.Add(parsed with { Path = path ?? "./", Timeout = wait, Interval = every, Retries = retries ?? parsed.Retries });
		}

		return true;
	}

	private static bool TryCheck(string value, out JourneyPrecondition precondition, out string problem)
	{
		problem = string.Empty;
		precondition = null!;
		var (index, length, match) = FindOperator(value);
		var target = index < 0 ? string.Empty : value[..index];
		var expected = index < 0 ? string.Empty : value[(index + length)..];
		if (index < 0 || target.Length == 0 || expected.Length == 0)
		{
			problem =
				$"--wait-for '{value}' is not a check: use header:Name==value, selector==value or selector@attribute==value ('=~' matches a regular expression, '=*' contains).";
			return false;
		}

		if (target.StartsWith("header:", StringComparison.OrdinalIgnoreCase))
		{
			var name = target["header:".Length..];
			if (name.Length == 0)
			{
				problem = $"--wait-for '{value}' names no header.";
				return false;
			}

			precondition = new JourneyPrecondition { Path = "./", Header = name, Expected = expected, Match = match };
			return true;
		}

		string? attribute = null;
		if (target.LastIndexOf('@') is var at and >= 0)
		{
			attribute = target[(at + 1)..];
			target = target[..at];
		}

		if (target.Length == 0 || attribute is { Length: 0 })
		{
			problem = $"--wait-for '{value}' is not a check: the selector or the attribute is empty.";
			return false;
		}

		precondition = new JourneyPrecondition
		{
			Path = "./",
			Selector = target,
			Attribute = attribute,
			Expected = expected,
			Match = match
		};
		return true;
	}

	private static (int Index, int Length, TextMatch Match) FindOperator(string value)
	{
		var best = (Index: -1, Length: 0, Match: TextMatch.Exact);
		foreach (var (token, match) in Operators)
		{
			var at = value.IndexOf(token, StringComparison.Ordinal);
			if (at >= 0 && (best.Index < 0 || at < best.Index))
				best = (at, token.Length, match);
		}

		return best;
	}

	/// <summary>Reads <c>10s</c>, <c>500ms</c>, <c>2m</c>, or a bare number of seconds.</summary>
	private static bool TryDuration(string flag, string? value, TimeSpan fallback, out TimeSpan duration, out string problem)
	{
		problem = string.Empty;
		duration = fallback;
		if (value is null)
			return true;
		var (digits, unit) = value.EndsWith("ms", StringComparison.Ordinal)
			? (value[..^2], 0.001)
			: value.EndsWith('s') ? (value[..^1], 1.0) : value.EndsWith('m') ? (value[..^1], 60.0) : (value, 1.0);
		if (double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && number > 0)
		{
			duration = TimeSpan.FromSeconds(number * unit);
			return true;
		}

		problem = $"{flag} '{value}' is not a duration: use 10s, 500ms or 2m.";
		return false;
	}
}
