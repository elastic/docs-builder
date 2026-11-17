// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Domain;

/// <summary>
/// How a domain operation hands back a record: built or derived, then checked by the aggregate's constraint set, as a <see cref="Result{T}"/>.
/// An operation is pure: no IO, no clock (the caller passes <c>today</c>), no collector (a finding is a diagnostic in the result).
/// </summary>
internal static class DomainOperation
{
	/// <summary>
	/// A record built from facts that were never in a file: every finding of <paramref name="rules"/> is in the result, so the caller sees what
	/// a loader would have reported. The value is carried even when a rule of Error severity is broken (see <see cref="Result{T}.HasErrors"/>).
	/// Violations are reported against <paramref name="file"/>, which the caller must name so no diagnostic carries an empty file.
	/// </summary>
	public static Result<T> Built<T>(
		Nullean.Entities.ConstraintSet<T> rules,
		T value,
		string file,
		IReadOnlyList<Diagnostic>? own = null
	) where T : class => Result.From(rules.Report(value), file, own);

	/// <summary>
	/// A record derived from one that was already checked: re-checked by <paramref name="rules"/>, and only the errors are in the result. The warnings
	/// the source carried were reported when it was read; what the derivation itself breaks is an error of its own, reported against <paramref name="file"/>.
	/// </summary>
	/// <remarks>
	/// <paramref name="own"/> keeps its findings at any severity, so a warning or hint of the operation is in <see cref="Result{T}.Diagnostics"/> and is
	/// written by <see cref="Result{T}.EmitTo"/>, but not by <see cref="Result{T}.EmitErrorsTo"/>.
	/// </remarks>
	public static Result<T> Derived<T>(
		Nullean.Entities.ConstraintSet<T> rules,
		T value,
		string file,
		IReadOnlyList<Diagnostic>? own = null
	) where T : class
	{
		var errors = rules
			.Report(value)
			.Violations
			.Where(v => v.Severity == Nullean.Entities.Severity.Error)
			.Select(v => v.ToDiagnostic(file));
		return Result.Success(value, [.. own ?? [], .. errors]);
	}
}
