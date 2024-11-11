// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst;
using Markdig.Helpers;
using Markdig.Parsers;

namespace Elastic.Markdown.Diagnostics;

public static class ProcessorDiagnosticExtensions
{
	public static void EmitError(this InlineProcessor processor, int line, int column, int length, string message)
	{
		var context = processor.GetContext();
		if (context.SkipValidation) return;
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = processor.GetContext().Path.FullName,
			Column = column,
			Line = line,
			Message = message,
			Length = length
		};
		context.Build.Collector.Channel.Write(d);
	}


	public static void EmitWarning(this BlockProcessor processor, int line, int column, int length, string message)
	{
		var context = processor.GetContext();
		if (context.SkipValidation) return;
		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = processor.GetContext().Path.FullName,
			Column = column,
			Line = line,
			Message = message,
			Length = length
		};
		context.Build.Collector.Channel.Write(d);
	}

	public static void EmitError(this ParserContext context, int line, int column, int length, string message)
	{
		if (context.SkipValidation) return;
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = context.Path.FullName,
			Column = column,
			Line = line,
			Message = message,
			Length = length
		};
		context.Build.Collector.Channel.Write(d);
	}

	public static void EmitWarning(this ParserContext context, int line, int column, int length, string message)
	{
		if (context.SkipValidation) return;
		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = context.Path.FullName,
			Column = column,
			Line = line,
			Message = message,
			Length = length
		};
		context.Build.Collector.Channel.Write(d);
	}
}
