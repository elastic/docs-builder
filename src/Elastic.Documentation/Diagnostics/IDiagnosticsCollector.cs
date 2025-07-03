// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;

namespace Elastic.Documentation.Diagnostics;

public interface IDiagnosticsCollector : IAsyncDisposable
{
	int Warnings { get; }
	int Errors { get; }
	int Hints { get; }

	ConcurrentBag<string> CrossLinks { get; }
	HashSet<string> OffendingFiles { get; }
	ConcurrentDictionary<string, bool> InUseSubstitutionKeys { get; }

	void Emit(Severity severity, string file, string message);
	void EmitError(string file, string message, Exception? e = null);
	void EmitWarning(string file, string message);
	void EmitHint(string file, string message);
	void Write(Diagnostic diagnostic);
	void CollectUsedSubstitutionKey(ReadOnlySpan<char> key);
	void EmitCrossLink(string link);
}

public static class DiagnosticsCollectorExtensions
{

	public static void Emit(this IDiagnosticsCollector collector, Severity severity, IFileInfo file, string message) =>
		collector.Emit(severity, file.FullName, message);

	public static void EmitError(this IDiagnosticsCollector collector, IFileInfo file, string message, Exception? e = null) =>
		collector.EmitError(file.FullName, message, e);

	public static void EmitWarning(this IDiagnosticsCollector collector, IFileInfo file, string message) =>
		collector.EmitWarning(file.FullName, message);

	public static void EmitHint(this IDiagnosticsCollector collector, IFileInfo file, string message) =>
		collector.EmitHint(file.FullName, message);

	/// Emit an error not associated with a file
	public static void EmitGlobalError(this IDiagnosticsCollector collector, string message, Exception? e = null) =>
		collector.EmitError(string.Empty, message, e);

	/// Emit a warning not associated with a file
	public static void EmitGlobalWarning(this IDiagnosticsCollector collector, string message) =>
		collector.EmitWarning(string.Empty, message);

	/// Emit a hint not associated with a file
	public static void EmitGlobalHint(this IDiagnosticsCollector collector, string message) =>
		collector.EmitHint(string.Empty, message);
}


