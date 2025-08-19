// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Elastic.Documentation.Diagnostics;

public interface IDiagnosticsCollector : IAsyncDisposable, IHostedService
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

	void EmitError(IFileInfo file, string message, Exception? e = null) => EmitError(file.FullName, message, e);

	void Emit(Severity severity, IFileInfo file, string message) => Emit(severity, file.FullName, message);

	void EmitWarning(IFileInfo file, string message) => EmitWarning(file.FullName, message);

	void EmitHint(IFileInfo file, string message) => EmitHint(file.FullName, message);

	/// Emit an error not associated with a file
	void EmitGlobalError(string message, Exception? e = null) => EmitError(string.Empty, message, e);

	/// Emit a warning not associated with a file
	void EmitGlobalWarning(string message) => EmitWarning(string.Empty, message);

	/// Emit a hint not associated with a file
	void EmitGlobalHint(string message) => EmitHint(string.Empty, message);

}
