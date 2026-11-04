// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;
using Markdig.Syntax;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Port of <c>MarkdownDocumentAssertions</c> from <c>MarkdownDocumentAssertions.fs</c>.
/// Operates on the materialized <see cref="Markdig.Syntax.MarkdownDocument"/> produced by
/// <see cref="AuthoringGenerator.GenerateAsync"/>.
/// </summary>
internal static class MarkdownDocumentAssertions
{
	/// <summary>
	/// Returns all elements of type <typeparamref name="T"/> found by a recursive Descendants
	/// walk of the fully-parsed document. Throws if none found.
	/// Port of F# <c>parses</c>.
	/// </summary>
	[DebuggerStepThrough]
	internal static Task<T[]> Parses<T>(MarkdownResult actual) where T : MarkdownObject
	{
		var found = actual.Document.Descendants<T>().ToArray();
		if (found.Length == 0)
			throw new AwesomeAssertions.Execution.AssertionFailedException($"Could not find {typeof(T).Name} in fully parsed document");
		return Task.FromResult(found);
	}

	/// <summary>
	/// Returns all elements of type <typeparamref name="T"/> found in the minimally-parsed document.
	/// Throws if none found.
	/// Port of F# <c>parsesMinimal</c>.
	/// </summary>
	[DebuggerStepThrough]
	internal static Task<T[]> ParsesMinimal<T>(MarkdownResult actual) where T : MarkdownObject
	{
		var found = actual.MinimalParse.Descendants<T>().ToArray();
		if (found.Length == 0)
			throw new AwesomeAssertions.Execution.AssertionFailedException($"Could not find {typeof(T).Name} in minimally parsed document");
		return Task.FromResult(found);
	}

	/// <summary>
	/// Asserts that the <c>index.md</c> file's <c>YamlFrontMatter.AppliesTo</c> equals
	/// <paramref name="expected"/>. Uses structural <c>Equals</c> — NOT <c>BeEquivalentTo</c>.
	///
	/// Mirrors F# <c>appliesTo</c>. Note the F# version copies the expected diagnostics onto
	/// the actual before comparison (so it is excluded from the diff); we do the same here.
	/// </summary>
	[DebuggerStepThrough]
	internal static void AppliesTo(ApplicableTo? expected, GeneratorResults results)
	{
		var result = results.MarkdownResults.FirstOrDefault(r => r.File.RelativePath == "index.md")
			?? throw new AwesomeAssertions.Execution.AssertionFailedException("Could not find 'index.md' in generator results");

		var matter = result.File.YamlFrontMatter
			?? throw new AwesomeAssertions.Execution.AssertionFailedException($"{result.File.RelativePath} has no YAML front matter");

		// Copy diagnostics from expected onto actual so the Equals comparison ignores them —
		// this mirrors the F# `applies.Diagnostics <- a.Diagnostics` line exactly.
		if (expected is not null && matter.AppliesTo is not null)
			matter.AppliesTo.Diagnostics = expected.Diagnostics;

		matter.AppliesTo.Should().Be(expected);
	}
}
