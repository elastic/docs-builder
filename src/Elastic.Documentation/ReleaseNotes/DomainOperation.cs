// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// How a domain operation hands back a record: built or derived, then checked by the aggregate's constraint set, as a <see cref="Result{T}"/>.
/// An operation is pure: no IO, no clock (the caller passes <c>today</c>), no collector (a finding is a diagnostic in the result).
/// </summary>
internal static class DomainOperation
{
	/// <summary>
	/// A record built from facts that were never in a file: every finding of <paramref name="rules"/> is in the result, so the caller sees what
	/// a loader would have reported. The value is carried even when a rule of Error severity is broken (see <see cref="Result{T}.HasErrors"/>).
	/// </summary>
	public static Result<T> Built<T>(
		Nullean.Entities.ConstraintSet<T> rules,
		T value,
		IReadOnlyList<Diagnostic>? own = null,
		string file = ""
	) where T : class => Result.From(rules.Report(value), file, own);

	/// <summary>
	/// A record derived from one that was already checked: re-checked by <paramref name="rules"/>, and only the errors are in the result. The warnings
	/// the source carried were reported when it was read; what the derivation itself breaks is an error of its own.
	/// </summary>
	public static Result<T> Derived<T>(
		Nullean.Entities.ConstraintSet<T> rules,
		T value,
		IReadOnlyList<Diagnostic>? own = null
	) where T : class
	{
		var errors = rules
			.Report(value)
			.Violations
			.Where(v => v.Severity == Nullean.Entities.Severity.Error)
			.Select(v => v.ToDiagnostic(string.Empty));
		return Result.Success(value, [.. own ?? [], .. errors]);
	}

	/// <summary>The lifecycle a CLI or label text names, null when blank or not a lifecycle.</summary>
	public static Lifecycle? ParseLifecycle(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;
		return LifecycleExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true) ? result : null;
	}
}
