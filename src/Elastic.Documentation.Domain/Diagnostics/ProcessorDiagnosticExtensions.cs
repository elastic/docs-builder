// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Markdown.Diagnostics;

public static class ProcessorDiagnosticExtensions
{
	public static void EmitError(this BuildContext context, IFileInfo file, string message, Exception? e = null)
	{
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = file.FullName,
			Message = message + (e != null ? Environment.NewLine + e : string.Empty),
		};
		context.Collector.Channel.Write(d);
	}

	public static void EmitWarning(this BuildContext context, IFileInfo file, string message)
	{
		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = file.FullName,
			Message = message,
		};
		context.Collector.Channel.Write(d);
	}

	public static void EmitError(this DiagnosticsCollector collector, IFileInfo file, string message, Exception? e = null)
	{
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = file.FullName,
			Message = message + (e != null ? Environment.NewLine + e : string.Empty),
		};
		collector.Channel.Write(d);
	}

	public static void EmitWarning(this DiagnosticsCollector collector, IFileInfo file, string message)
	{
		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = file.FullName,
			Message = message,
		};
		collector.Channel.Write(d);
	}

}
