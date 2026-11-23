// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Diagnostics;

/// <summary>A record that could be read, with every finding made while reading and checking it. A tolerant load may still hold errors.</summary>
public sealed record Loaded<T>(T Value, IReadOnlyList<Diagnostic> Findings) where T : class;

/// <summary>A load that produced no record, with the findings that say why.</summary>
public sealed record Unreadable<T>(IReadOnlyList<Diagnostic> Findings) where T : class;

/// <summary>
/// The outcome of loading: a <see cref="Loaded{T}"/> or an <see cref="Unreadable{T}"/>. Loaders return this instead of throwing or writing to a collector;
/// callers forward the findings to their <see cref="IDiagnosticsCollector"/> with <see cref="LoadResultExtensions.Report{T}"/> and then match on the case,
/// or use <see cref="LoadResultExtensions.OrThrow{T}"/>. A <c>default</c> value holds no case: its <c>Value</c> is null and its extensions throw.
/// </summary>
public readonly union LoadResult<T>(Loaded<T>, Unreadable<T>) where T : class;

public static class LoadResultExtensions
{
	/// <summary>Every finding, in the order found.</summary>
	public static IReadOnlyList<Diagnostic> Findings<T>(this LoadResult<T> result) where T : class => result switch
	{
		Loaded<T> loaded => loaded.Findings,
		Unreadable<T> unreadable => unreadable.Findings,
		_ => throw new InvalidOperationException("LoadResult has no value")
	};

	public static bool HasErrors<T>(this LoadResult<T> result) where T : class => result.Findings().Any(d => d.Severity == Severity.Error);

	/// <summary>The loaded record, null when the load is <see cref="Unreadable{T}"/>.</summary>
	public static T? ValueOrNull<T>(this LoadResult<T> result) where T : class => result is Loaded<T> loaded ? loaded.Value : null;

	/// <summary>
	/// Writes every finding to <paramref name="collector"/>, in order, and returns this result, so the caller chooses what to do with it:
	/// <c>.OrThrow()</c>, <c>.ValueOrNull()</c>, or a match on the case.
	/// </summary>
	public static LoadResult<T> Report<T>(this LoadResult<T> result, IDiagnosticsCollector collector) where T : class
	{
		foreach (var diagnostic in result.Findings())
			collector.Write(diagnostic);
		return result;
	}

	/// <summary>Shorthand for <c>Report(collector).ValueOrNull()</c>: writes every finding and returns the record, if there is one.</summary>
	public static T? EmitTo<T>(this LoadResult<T> result, IDiagnosticsCollector collector) where T : class =>
		result.Report(collector).ValueOrNull();

	/// <summary>
	/// Writes only the error findings to <paramref name="collector"/> and returns the record when there are none (null otherwise). For a caller that
	/// derived a record from one the loader already checked: the warnings it carried were reported when it was read, so only what the derivation
	/// itself broke is news. Warnings and hints that a result carries of its own are not written here: they are in <see cref="Findings{T}"/> and are
	/// written by <see cref="EmitTo{T}"/>.
	/// </summary>
	public static T? EmitErrorsTo<T>(this LoadResult<T> result, IDiagnosticsCollector collector) where T : class
	{
		var failed = false;
		foreach (var diagnostic in result.Findings().Where(d => d.Severity == Severity.Error))
		{
			collector.Write(diagnostic);
			failed = true;
		}
		return failed ? null : result.ValueOrNull();
	}

	/// <summary>
	/// For inputs whose errors are reported but do not stop the command (a docset): returns the record even when there
	/// are errors, and throws only when none could be produced, with the last error's message.
	/// </summary>
	public static T OrThrowIfMissing<T>(this LoadResult<T> result) where T : class =>
		result.ValueOrNull()
			?? throw new InvalidOperationException(
				result.Findings().Where(d => d.Severity == Severity.Error).Select(d => d.Message).LastOrDefault() ??
					$"Could not load {typeof(T).Name}"
			);

	/// <summary>
	/// For configuration that must load for the process to start (global config): returns the record, or throws a
	/// <see cref="ConfigurationException"/> whose message lists every error (one per line) and which carries every finding.
	/// </summary>
	public static T OrThrow<T>(this LoadResult<T> result) where T : class =>
		result is Loaded<T> loaded && !result.HasErrors()
			? loaded.Value
			: throw new ConfigurationException(result.Findings(), $"Could not load {typeof(T).Name}");
}
