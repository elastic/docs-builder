// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.Directives;
using Markdig.Parsers;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Diagnostics;

public static class ProcessorDiagnosticExtensions
{
	private static string CreateExceptionMessage(string message, Exception? e) => message + (e != null ? Environment.NewLine + e : string.Empty);

	public static void EmitError(this InlineProcessor processor, int line, int column, int length, string message)
	{
		var context = processor.GetContext();
		if (context.SkipValidation)
			return;
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = processor.GetContext().MarkdownSourcePath.FullName,
			Column = column,
			Line = line,
			Message = message,
			Length = length
		};
		context.Build.Collector.Channel.Write(d);
	}


	public static void EmitWarning(this InlineProcessor processor, int line, int column, int length, string message)
	{
		var context = processor.GetContext();
		if (context.SkipValidation)
			return;
		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = processor.GetContext().MarkdownSourcePath.FullName,
			Column = column,
			Line = line,
			Message = message,
			Length = length
		};
		context.Build.Collector.Channel.Write(d);
	}

	public static void EmitError(this ParserContext context, string message, Exception? e = null)
	{
		if (context.SkipValidation)
			return;
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = context.MarkdownSourcePath.FullName,
			Message = CreateExceptionMessage(message, e),
		};
		context.Build.Collector.Channel.Write(d);
	}

	public static void EmitWarning(this ParserContext context, int line, int column, int length, string message)
	{
		if (context.SkipValidation)
			return;
		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = context.MarkdownSourcePath.FullName,
			Column = column,
			Line = line,
			Message = message,
			Length = length
		};
		context.Build.Collector.Channel.Write(d);
	}

	public static void EmitError(this BuildContext context, IFileInfo file, string message, Exception? e = null)
	{
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = file.FullName,
			Message = CreateExceptionMessage(message, e),
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
			Message = CreateExceptionMessage(message, e),
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

	public static void EmitError(this IBlockExtension block, string message, Exception? e = null) => EmitDiagnostic(block, Severity.Error, message, e);

	public static void EmitWarning(this IBlockExtension block, string message) => EmitDiagnostic(block, Severity.Warning, message);

	public static void EmitHint(this IBlockExtension block, string message) => EmitDiagnostic(block, Severity.Hint, message);

	private static void EmitDiagnostic(IBlockExtension block, Severity severity, string message, Exception? e = null)
	{
		if (block.SkipValidation)
			return;

		var d = new Diagnostic
		{
			Severity = severity,
			File = block.CurrentFile.FullName,
			Line = block.Line + 1,
			Column = block.Column,
			Length = block.OpeningLength + 5,
			Message = CreateExceptionMessage(message, e),
		};
		block.Build.Collector.Channel.Write(d);
	}


	private static void LinkDiagnostic(InlineProcessor processor, Severity severity, Inline inline, int length, string message, Exception? e = null)
	{
		var line = inline.Line + 1;
		var column = inline.Column;

		var context = processor.GetContext();
		if (context.SkipValidation)
			return;
		var d = new Diagnostic
		{
			Severity = severity,
			File = processor.GetContext().MarkdownSourcePath.FullName,
			Column = Math.Max(column, 1),
			Line = line,
			Message = CreateExceptionMessage(message, e),
			Length = Math.Max(length, 1)
		};
		context.Build.Collector.Channel.Write(d);
	}

	public static void EmitError(this InlineProcessor processor, LinkInline inline, string message) =>
		LinkDiagnostic(processor, Severity.Error, inline, inline.Url?.Length ?? 1, message);

	public static void EmitWarning(this InlineProcessor processor, LinkInline inline, string message) =>
		LinkDiagnostic(processor, Severity.Warning, inline, inline.Url?.Length ?? 1, message);

	public static void EmitHint(this InlineProcessor processor, LinkInline inline, string message) =>
		LinkDiagnostic(processor, Severity.Hint, inline, inline.Url?.Length ?? 1, message);

	public static void EmitError(this InlineProcessor processor, Inline inline, int length, string message, Exception? e = null) =>
		LinkDiagnostic(processor, Severity.Error, inline, length, message, e);

	public static void EmitWarning(this InlineProcessor processor, Inline inline, int length, string message) =>
		LinkDiagnostic(processor, Severity.Warning, inline, length, message);

	public static void EmitHint(this InlineProcessor processor, Inline inline, int length, string message) =>
		LinkDiagnostic(processor, Severity.Hint, inline, length, message);
}
