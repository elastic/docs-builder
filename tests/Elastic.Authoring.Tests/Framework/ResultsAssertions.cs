// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text.Json;
using JetBrains.Annotations;
using Xunit.Sdk;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Port of <c>ResultsAssertions</c> and <c>JsonAssertions</c> from
/// <c>MarkdownResultsAssertions.fs</c>.
/// </summary>
internal static class ResultsAssertions
{
	/// <summary>
	/// Finds the <see cref="MarkdownResult"/> for <paramref name="path"/> in the generator output.
	/// Mirrors F# <c>converts</c>. Called from <see cref="Scenario.Converts(string)"/>.
	/// </summary>
	[DebuggerStepThrough]
	internal static MarkdownResult Finds(string path, GeneratorResults results)
	{
		var normalized = path.Replace('/', Path.DirectorySeparatorChar);
		var result = results.MarkdownResults.FirstOrDefault(m => m.File.RelativePath == normalized);

		if (result is null)
			throw new XunitException($"{path} not part of the markdown results");

		return result;
	}

	/// <summary>
	/// Asserts that the file at <paramref name="artifactPath"/> in the write file system
	/// contains JSON that normalizes to <paramref name="expected"/> (pretty-printed).
	/// Port of F# <c>convertsToJson</c>.
	/// </summary>
	[DebuggerStepThrough]
	internal static void ConvertsToJson(string artifactPath, [LanguageInjection("json")] string expected, GeneratorResults results)
	{
		var fs = results.Context.ReadFileSystem;
		var fi = fs.FileInfo.New(artifactPath);
		if (!fi.Exists)
			throw new XunitException($"{artifactPath} is not part of the output");

		var actualRaw = fs.File.ReadAllText(fi.FullName);
		using var actualDoc = JsonDocument.Parse(actualRaw);
		var actualPretty = JsonSerializer.Serialize(actualDoc, new JsonSerializerOptions { WriteIndented = true });

		using var expectedDoc = JsonDocument.Parse(expected);
		var expectedPretty = JsonSerializer.Serialize(expectedDoc, new JsonSerializerOptions { WriteIndented = true });

		var diff = HtmlAssertions.Diff(expectedPretty, actualPretty);
		if (!string.IsNullOrEmpty(diff))
			throw new XunitException($"JSON was not equal\n-- DIFF --\n{diff}");
	}
}
