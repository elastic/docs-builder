// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Diagnostics;

/// <summary>A violation of a domain constraint (Nullean.Entities) as the diagnostic the build reports.</summary>
public static class ViolationDiagnostics
{
	/// <summary>
	/// As a diagnostic carrying the rule's id as <see cref="Diagnostic.RuleId"/>. Points at the violation's own file, line, column and length
	/// when the rule set them, otherwise at <paramref name="file"/>, the file the model was declared in.
	/// </summary>
	public static Diagnostic ToDiagnostic(this Violation violation, string file) =>
		new()
		{
			Severity = violation.Severity switch
			{
				Nullean.Entities.Severity.Error => Severity.Error,
				Nullean.Entities.Severity.Warning => Severity.Warning,
				_ => Severity.Hint
			},
			File = violation.File ?? file,
			Message = violation.Message,
			Line = violation.Line,
			Column = violation.Column,
			Length = violation.Length,
			RuleId = violation.ConstraintId
		};

	/// <summary>Every violation of the report as a diagnostic, in the order the set reported them.</summary>
	public static IReadOnlyList<Diagnostic> ToDiagnostics<T>(this ConstraintReport<T> report, string file) =>
		[.. report.Violations.Select(v => v.ToDiagnostic(file))];

	/// <summary>Writes every violation of <paramref name="report"/> to the collector and returns whether the value is usable (no errors).</summary>
	public static bool Write<T>(this IDiagnosticsCollector collector, ConstraintReport<T> report, string file)
	{
		foreach (var diagnostic in report.ToDiagnostics(file))
			collector.Write(diagnostic);
		return !report.HasErrors;
	}
}
