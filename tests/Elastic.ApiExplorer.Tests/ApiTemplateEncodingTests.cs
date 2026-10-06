// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;

namespace Elastic.ApiExplorer.Tests;

public partial class ApiTemplateEncodingTests
{
	// These predate the check and render on every build. Do not add to this list: write new characters as entities.
	private static readonly string[] KnownFiles = ["_ApiBreadcrumbs.cshtml", "_UnionOptions.cshtml", "_PropertyItem.cshtml"];

	/// <summary>
	/// Streaming a page that had <c>‹ › ’ “ ” →</c> typed into its Razor templates hung the API build forever at 100% CPU
	/// (RazorSlices 0.11.1, on <c>indices-analyze</c>, whose page carries seven examples). Rendering the same page to a
	/// string was fine, and swapping the characters for HTML entities fixed the stream. Use <c>&amp;rsaquo;</c>-style entities.
	/// </summary>
	[Test]
	public void Templates_DoNotTypeMultiByteCharactersIntoRenderedMarkup()
	{
		var root = Path.Join(Paths.WorkingDirectoryRoot.FullName, "src", "Elastic.ApiExplorer");
		var offenders = Directory
			.EnumerateFiles(root, "*.cshtml", SearchOption.AllDirectories)
			.Where(file => !KnownFiles.Contains(Path.GetFileName(file)))
			.SelectMany(
				file => RenderedLines(file)
					.Where(static l => NonAscii().IsMatch(l.Text))
					.Select(l => $"{Path.GetRelativePath(root, file)}:{l.Number}  {l.Text.Trim()}")
			)
			.ToArray();

		offenders.Should().BeEmpty(
			"write these characters as HTML entities (&rsaquo; &rsquo; &ldquo; &rarr;): a streamed page that contained them hung the build"
		);
	}

	private static IEnumerable<(int Number, string Text)> RenderedLines(string file)
	{
		// Razor comments are never rendered, so they may hold any character.
		var withoutComments = RazorComment().Replace(
			File.ReadAllText(file),
			static m => new string('\n', m.Value.Count(static c => c == '\n'))
		);
		return withoutComments.Split('\n').Select(static (text, i) => (i + 1, text));
	}

	[GeneratedRegex(@"@\*.*?\*@", RegexOptions.Singleline)]
	private static partial Regex RazorComment();

	[GeneratedRegex(@"[^\x00-\x7F]")]
	private static partial Regex NonAscii();
}
