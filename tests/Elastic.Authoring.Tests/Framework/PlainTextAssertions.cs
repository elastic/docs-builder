// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Markdown.Exporters;
using JetBrains.Annotations;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Port of <c>PlainTextAssertions</c> from <c>PlainTextAssertions.fs</c>.
/// </summary>
internal static class PlainTextAssertions
{
	internal static string ToPlainText(MarkdownResult actual) =>
		PlainTextExporter.ConvertToPlainText(actual.Document, actual.Context.Generator.Context).Trim();

	[DebuggerStepThrough]
	internal static void ConvertsToPlainText([LanguageInjection("text")] string? expected, GeneratorResults results, CallSite site)
	{
		var defaultFile = results.MarkdownResults.FirstOrDefault(r => r.File.RelativePath == "index.md")
			?? throw new AwesomeAssertions.Execution.AssertionFailedException("Could not find 'index.md' in generator results");

		InlineSnapshots.Text(ToPlainText(defaultFile), expected, nameof(Scenario.ConvertsToPlainText), site);
	}
}
