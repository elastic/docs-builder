// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Diagnostics;
using Xunit.Sdk;

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
			throw new XunitException(
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
			throw new XunitException("Expected errors but no errors were logged");

		// Only the first error — mirrors F# List.tryHead
		var errorDiagnostics = results.Context.Collector.Diagnostics.Where(d => d.Severity == Severity.Error).ToList();

		if (errorDiagnostics.Count == 0)
			throw new XunitException("Expected errors but no errors were logged");

		var first = errorDiagnostics[0];
		if (!first.Message.Contains(expected, StringComparison.Ordinal))
			throw new XunitException($"Expected error containing '{expected}' but first error was: {first.Message}");
	}

	[DebuggerStepThrough]
	internal static void HasNoWarnings(GeneratorResults results)
	{
		var warnings = results.Context.Collector.Warnings;
		if (warnings != 0)
			throw new XunitException(
				$"Expected no warnings but found {warnings}: " + string.Join(
					"; ",
					results.Context.Collector.Diagnostics.Where(d => d.Severity == Severity.Warning).Select(d => d.Message)
				)
			);
	}

	[DebuggerStepThrough]
	internal static void HasWarning(string expected, GeneratorResults results)
	{
		var warnings = results.Context.Collector.Warnings;
		if (warnings == 0)
			throw new XunitException("Expected warnings but no warnings were logged");

		// Only the first warning — mirrors F# List.tryHead
		var warningDiagnostics = results.Context.Collector.Diagnostics.Where(d => d.Severity == Severity.Warning).ToList();

		if (warningDiagnostics.Count == 0)
			throw new XunitException("Expected warnings but no warnings were logged");

		var first = warningDiagnostics[0];
		if (!first.Message.Contains(expected, StringComparison.Ordinal))
			throw new XunitException($"Expected warning containing '{expected}' but first warning was: {first.Message}");
	}

	[DebuggerStepThrough]
	internal static void HasHint(string expected, GeneratorResults results)
	{
		var hints = results.Context.Collector.Hints;
		if (hints == 0)
			throw new XunitException("Expected hints but no hints were logged");

		// Only the first hint — mirrors F# List.tryHead
		var hintDiagnostics = results.Context.Collector.Diagnostics.Where(d => d.Severity == Severity.Hint).ToList();

		if (hintDiagnostics.Count == 0)
			throw new XunitException("Expected hints but no hints were logged");

		var first = hintDiagnostics[0];
		if (!first.Message.Contains(expected, StringComparison.Ordinal))
			throw new XunitException($"Expected hint containing '{expected}' but first hint was: {first.Message}");
	}
}
