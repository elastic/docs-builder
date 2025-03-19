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
			Message = message + (e != null ? Environment.NewLine + e : string.Empty),
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

	public static void EmitError(this IBlockExtension block, string message, Exception? e = null)
	{
		if (block.SkipValidation)
			return;

		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = block.CurrentFile.FullName,
			Line = block.Line + 1,
			Column = block.Column,
			Length = block.OpeningLength + 5,
			Message = message + (e != null ? Environment.NewLine + e : string.Empty),
		};
		block.Build.Collector.Channel.Write(d);
	}

	public static void EmitWarning(this IBlockExtension block, string message)
	{
		if (block.SkipValidation)
			return;

		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = block.CurrentFile.FullName,
			Line = block.Line + 1,
			Column = block.Column,
			Length = block.OpeningLength + 4,
			Message = message
		};
		block.Build.Collector.Channel.Write(d);
	}

	private static void LinkDiagnostic(InlineProcessor processor, Severity severity, LinkInline inline, string message)
	{
		var url = inline.Url;
		var line = inline.Line + 1;
		var column = inline.Column;
		var length = url?.Length ?? 1;

		var context = processor.GetContext();
		if (context.SkipValidation)
			return;
		var d = new Diagnostic
		{
			Severity = severity,
			File = processor.GetContext().MarkdownSourcePath.FullName,
			Column = Math.Max(column, 1),
			Line = line,
			Message = message,
			Length = length
		};
		context.Build.Collector.Channel.Write(d);
	}

	public static void EmitError(this InlineProcessor processor, LinkInline inline, string message) =>
		LinkDiagnostic(processor, Severity.Error, inline, message);

	public static void EmitWarning(this InlineProcessor processor, LinkInline inline, string message) =>
		LinkDiagnostic(processor, Severity.Warning, inline, message);

	public static void EmitHint(this InlineProcessor processor, LinkInline inline, string message) =>
		LinkDiagnostic(processor, Severity.Hint, inline, message);
}
