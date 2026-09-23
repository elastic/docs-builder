// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using Elastic.Documentation.AppliesTo;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.Myst.Components;
using JetBrains.Annotations;
using Xunit.Sdk;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Port of <c>LlmMarkdownAssertions</c> from <c>LlmMarkdownAssertions.fs</c>.
///
/// WARNING: <see cref="ToLlmMarkdownWithMetadata"/> duplicates production front-matter
/// rendering logic from <c>LlmMarkdownExporter</c>. It was added in the F# suite for
/// exactly one test. Do not expand it. File a follow-up to assert against the real exporter
/// path or delete the test once there is a better mechanism.
/// </summary>
internal static class LlmMarkdownAssertions
{
	internal static string ToLlmMarkdown(MarkdownResult actual) =>
		LlmMarkdownExporter.ConvertToLlmMarkdown(actual.Document, actual.Context.Generator.Context).Trim();

	/// <summary>
	/// Builds the full LLM Markdown output with front-matter metadata (title, applies_to, etc.).
	/// Mirrors F# <c>toLlmMarkdownWithMetadata</c> verbatim — including the duplication of
	/// production rendering logic. Do NOT fix this in the porting PR.
	/// </summary>
	internal static string ToLlmMarkdownWithMetadata(MarkdownResult actual)
	{
		var sourceFile = actual.File;
		var buildContext = actual.Context.Generator.Context;
		var llmBody = LlmMarkdownExporter.ConvertToLlmMarkdown(actual.Document, buildContext).Trim();

		var metadata = new StringBuilder();
		metadata.AppendLine("---");
		metadata.AppendLine($"title: {sourceFile.Title}");

		var frontMatter = sourceFile.YamlFrontMatter;
		if (frontMatter?.AppliesTo is { } appliesTo && appliesTo != ApplicableTo.All && appliesTo != ApplicableTo.Default)
		{
			var viewModel = new ApplicableToViewModel
			{
				AppliesTo = appliesTo,
				Inline = true,
				ShowTooltip = true,
				VersionsConfig = buildContext.VersionsConfiguration
			};

			var items = viewModel.GetApplicabilityItems();
			if (items.Count > 0)
			{
				metadata.AppendLine("applies_to:");
				foreach (var item in items)
				{
					var displayName = item.ApplicabilityDefinition.DisplayName.Replace("&nbsp;", " ");
					var popoverData = item.RenderData.PopoverData;
					var availabilityText = popoverData?.AvailabilityItems.Length > 0
						? string.Join(", ", popoverData.AvailabilityItems.Select(a => a.Text))
						: "Available";
					metadata.AppendLine($"  - {displayName}: {availabilityText}");
				}
			}
		}

		metadata.AppendLine("---");
		metadata.AppendLine();
		metadata.AppendLine($"# {sourceFile.Title}");
		metadata.Append(llmBody);
		return metadata.ToString().Trim();
	}

	[DebuggerStepThrough]
	internal static void ConvertsToNewLlm([LanguageInjection("markdown")] string expected, GeneratorResults results)
	{
		var defaultFile = results.MarkdownResults.FirstOrDefault(r => r.File.RelativePath == "index.md")
			?? throw new XunitException("Could not find 'index.md' in generator results");

		var actual = ToLlmMarkdown(defaultFile);
		var expectedTrimmed = expected.Trim();
		var difference = HtmlAssertions.Diff(expectedTrimmed, actual);

		if (string.IsNullOrEmpty(difference))
			return;

		throw new XunitException(
			$"""
LLM text was not equal
-- DIFF --
{difference}

-- EXPECTED --
{expectedTrimmed}

-- ACTUAL --
{actual}
"""
		);
	}

	[DebuggerStepThrough]
	internal static void ConvertsToLlmWithMetadata([LanguageInjection("markdown")] string expected, GeneratorResults results)
	{
		var defaultFile = results.MarkdownResults.FirstOrDefault(r => r.File.RelativePath == "index.md")
			?? throw new XunitException("Could not find 'index.md' in generator results");

		var actual = ToLlmMarkdownWithMetadata(defaultFile);
		var expectedTrimmed = expected.Trim();
		var difference = HtmlAssertions.Diff(expectedTrimmed, actual);

		if (string.IsNullOrEmpty(difference))
			return;

		throw new XunitException(
			$"""
LLM metadata output was not equal
-- DIFF --
{difference}

-- EXPECTED --
{expectedTrimmed}

-- ACTUAL --
{actual}
"""
		);
	}
}
