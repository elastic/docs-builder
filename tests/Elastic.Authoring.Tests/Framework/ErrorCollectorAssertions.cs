// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Port of <c>DiagnosticsCollectorAssertions</c> from <c>ErrorCollectorAssertions.fs</c>.
///
/// IMPORTANT — first-diagnostic-only semantics: exactly as in the F# original, each
/// <c>Has*</c> method inspects only the <em>first</em> diagnostic of the requested severity
/// (the F# equivalent is <c>List.tryHead</c>). This is intentional — multi-diagnostic tests
/// use separate <c>Fact</c>s, not a single composite assertion. Do not "fix" this in the
/// porting PR; change it only when accompanied by test updates.
///
/// Note: <see cref="Diagnostic"/> is a <c>readonly record struct</c> (value type), so
/// <c>FirstOrDefault</c> returns <c>default(Diagnostic)</c>, not null. We use
/// <c>Where().ToList()</c> + index access to avoid that ambiguity.
/// </summary>
internal static class ErrorCollectorAssertions
{
	[DebuggerStepThrough]
	internal static void HasNoErrors(GeneratorResults results)
	{
		var errors = results.Context.Collector.Errors;
		if (errors != 0)
			throw new AwesomeAssertions.Execution.AssertionFailedException(
				$"Expected no errors but found {errors}: " + string.Join(
					"; ",
					results.Context.Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message)
				)
			);
	}

	[DebuggerStepThrough]
	internal static void HasError(string expected, GeneratorResults results)
	{
		var errors = results.Context.Collector.Errors;
		if (errors == 0)
			throw new AwesomeAssertions.Execution.AssertionFailedException("Expected errors but no errors were logged");

		// Only the first error — mirrors F# List.tryHead
		var errorDiagnostics = results.Context.Collector.Diagnostics.Where(d => d.Severity == Severity.Error).ToList();

		if (errorDiagnostics.Count == 0)
			throw new AwesomeAssertions.Execution.AssertionFailedException("Expected errors but no errors were logged");

		var first = errorDiagnostics[0];
		if (!first.Message.Contains(expected, StringComparison.Ordinal))
			throw new AwesomeAssertions.Execution.AssertionFailedException(
				$"Expected error containing '{expected}' but first error was: {first.Message}"
			);
	}

	// The shared docs-tests redirects fixture deliberately contains a self-redirect and a redirect with several 'many' targets,
	// which the redirect constraints report as warnings in every scenario. They are not what these assertions are about.
	private static bool IsFixtureRedirectWarning(Diagnostic d) =>
		// the fixture has no git origin: the link-manifest rule that surfaces as a warning for now
		d.RuleId
			== LinkManifestConstraints.LinkManifestOriginNamed.Name
			|| d.Message.StartsWith("Redirect 'testing/redirects/", StringComparison.Ordinal)
				&& (d.Message.EndsWith("points to itself and is dropped.", StringComparison.Ordinal)
					|| d.Message.EndsWith("only the first with a 'to' is used.", StringComparison.Ordinal));

	private static List<Diagnostic> ScenarioWarnings(GeneratorResults results) =>
		results.Context.Collector.Diagnostics.Where(d => d.Severity == Severity.Warning && !IsFixtureRedirectWarning(d)).ToList();

	[DebuggerStepThrough]
	internal static void HasNoWarnings(GeneratorResults results)
	{
		var warnings = ScenarioWarnings(results);
		if (warnings.Count != 0)
			throw new AwesomeAssertions.Execution.AssertionFailedException(
				$"Expected no warnings but found {warnings.Count}: " + string.Join("; ", warnings.Select(d => d.Message))
			);
	}

	[DebuggerStepThrough]
	internal static void HasWarning(string expected, GeneratorResults results)
	{
		// Only the first warning — mirrors F# List.tryHead
		var warningDiagnostics = ScenarioWarnings(results);

		if (warningDiagnostics.Count == 0)
			throw new AwesomeAssertions.Execution.AssertionFailedException("Expected warnings but no warnings were logged");

		var first = warningDiagnostics[0];
		if (!first.Message.Contains(expected, StringComparison.Ordinal))
			throw new AwesomeAssertions.Execution.AssertionFailedException(
				$"Expected warning containing '{expected}' but first warning was: {first.Message}"
			);
	}

	[DebuggerStepThrough]
	internal static void HasHint(string expected, GeneratorResults results)
	{
		var hints = results.Context.Collector.Hints;
		if (hints == 0)
			throw new AwesomeAssertions.Execution.AssertionFailedException("Expected hints but no hints were logged");

		// Only the first hint — mirrors F# List.tryHead
		var hintDiagnostics = results.Context.Collector.Diagnostics.Where(d => d.Severity == Severity.Hint).ToList();

		if (hintDiagnostics.Count == 0)
			throw new AwesomeAssertions.Execution.AssertionFailedException("Expected hints but no hints were logged");

		var first = hintDiagnostics[0];
		if (!first.Message.Contains(expected, StringComparison.Ordinal))
			throw new AwesomeAssertions.Execution.AssertionFailedException(
				$"Expected hint containing '{expected}' but first hint was: {first.Message}"
			);
	}
}
