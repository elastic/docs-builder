// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Diagnostics;

/// <summary>
/// The outcome of loading, resolving or validating: the value, when one could be produced, plus every diagnostic found
/// while producing it. Loaders and resolvers return this instead of throwing or writing to a collector; callers forward
/// <see cref="Diagnostics"/> to their <see cref="IDiagnosticsCollector"/>.
/// </summary>
public sealed record Result<T>(T? Value, IReadOnlyList<Diagnostic> Diagnostics) where T : class
{
	public bool HasErrors => Diagnostics.Any(d => d.Severity == Severity.Error);

	/// <summary>Writes every diagnostic to <paramref name="collector"/>, in order, and returns the value.</summary>
	public T? EmitTo(IDiagnosticsCollector collector)
	{
		foreach (var diagnostic in Diagnostics)
			collector.Write(diagnostic);
		return Value;
	}

	/// <summary>
	/// For inputs whose errors are reported but do not stop the command (a docset): returns the value even when there
	/// are errors, and throws only when none could be produced, with the last error's message.
	/// </summary>
	public T OrThrowIfMissing() =>
		Value
			?? throw new InvalidOperationException(
				Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message).LastOrDefault() ??
					$"Could not load {typeof(T).Name}"
			);

	/// <summary>
	/// For configuration that must load for the process to start (global config): returns the value, or throws a
	/// <see cref="ConfigurationException"/> whose message lists every error (one per line) and which carries every diagnostic.
	/// </summary>
	public T OrThrow()
	{
		if (Value is not null && !HasErrors)
			return Value;
		throw new ConfigurationException(Diagnostics, $"Could not load {typeof(T).Name}");
	}
}

/// <summary>Factories for <see cref="Result{T}"/>.</summary>
public static class Result
{
	public static Result<T> Success<T>(T value) where T : class => new(value, []);

	public static Result<T> Success<T>(T value, IReadOnlyList<Diagnostic> diagnostics) where T : class => new(value, diagnostics);

	public static Result<T> Failure<T>(IReadOnlyList<Diagnostic> diagnostics) where T : class => new(null, diagnostics);

	/// <summary>
	/// The outcome of checking a model: always carries the checked value and every violation as a diagnostic (after <paramref name="extra"/>,
	/// the findings made while building it). When the report has errors the result <see cref="Result{T}.HasErrors"/>, so <see cref="Result{T}.OrThrow"/>
	/// throws and <see cref="Result{T}.EmitTo"/> reports them, while a caller that tolerates errors (a docset) can still use the value.
	/// </summary>
	public static Result<T> From<T>(ConstraintReport<T> report, string file, IReadOnlyList<Diagnostic>? extra = null) where T : class =>
		new(report.Value, [.. extra ?? [], .. report.ToDiagnostics(file)]);
}
