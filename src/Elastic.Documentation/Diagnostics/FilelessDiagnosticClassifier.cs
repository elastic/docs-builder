// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Diagnostics;

/// <summary>
/// Groups file-less diagnostics so console rendering can distinguish unhandled exceptions
/// from ordinary errors, warnings, and hints.
/// </summary>
public readonly record struct FilelessDiagnosticGroups(
	Diagnostic[] Exceptions,
	Diagnostic[] Errors,
	Diagnostic[] Warnings,
	Diagnostic[] Hints
)
{
	public bool IsEmpty => Exceptions.Length == 0 && Errors.Length == 0 && Warnings.Length == 0 && Hints.Length == 0;
}

public static class FilelessDiagnosticClassifier
{
	/// <summary>
	/// True when an error message includes an exception dump (e.g. <c>Exception.ToString()</c>
	/// appended by <see cref="DiagnosticsCollector.EmitError(string, string, Exception?)"/>).
	/// </summary>
	public static bool LooksLikeException(Diagnostic diagnostic) =>
		diagnostic.Severity == Severity.Error && diagnostic.Message.Contains("Exception:", StringComparison.Ordinal);

	public static FilelessDiagnosticGroups Group(IEnumerable<Diagnostic> diagnostics)
	{
		var exceptions = new List<Diagnostic>();
		var errors = new List<Diagnostic>();
		var warnings = new List<Diagnostic>();
		var hints = new List<Diagnostic>();

		foreach (var diagnostic in diagnostics)
		{
			if (!string.IsNullOrEmpty(diagnostic.File))
				continue;

			switch (diagnostic.Severity)
			{
				case Severity.Error when LooksLikeException(diagnostic):
					exceptions.Add(diagnostic);
					break;
				case Severity.Error:
					errors.Add(diagnostic);
					break;
				case Severity.Warning:
					warnings.Add(diagnostic);
					break;
				case Severity.Hint:
					hints.Add(diagnostic);
					break;
				default:
					break;
			}
		}

		return new FilelessDiagnosticGroups([.. exceptions], [.. errors], [.. warnings], [.. hints]);
	}
}
