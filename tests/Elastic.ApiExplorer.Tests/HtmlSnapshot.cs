// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// Renders markdown as a deterministic <c>&lt;p&gt;</c> wrapper so snapshots cover description plumbing
/// without depending on the full markdown pipeline. <see cref="NoopMarkdownStringRenderer"/> would blank
/// all descriptions and hide regressions.
/// </summary>
public sealed class PassthroughMarkdownRenderer : IMarkdownStringRenderer
{
	private PassthroughMarkdownRenderer() { }

	public static PassthroughMarkdownRenderer Instance { get; } = new();

	public string Render(string markdown, IFileInfo? source) => $"<p>{markdown}</p>";
}

public static class HtmlSnapshot
{
	private const string UpdateEnvironmentVariable = "UPDATE_SNAPSHOTS";

	/// <summary>
	/// Renders a page exactly like <c>OpenApiGenerator.Render</c> does: full slice (layout included, since
	/// layout creation triggers view model side effects) written to an in-memory file stream.
	/// </summary>
	public static async Task<string> RenderPageAsync(IApiModel model, INavigationItem current, BuildContext context, OpenApiDocument document, Cancel ctx = default)
	{
		var renderContext = new ApiRenderContext(context, document, new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};
		var fileSystem = new MockFileSystem();
		var outputFile = fileSystem.Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "out.html");
		await using (var stream = fileSystem.FileStream.New(outputFile, FileMode.Create))
			await model.RenderAsync(stream, renderContext, ctx);
		return fileSystem.File.ReadAllText(outputFile);
	}

	/// <summary>
	/// Extracts the page body section (balanced <c>&lt;section&gt;</c> scan) so snapshots exclude layout
	/// chrome that varies across branches: git branch/sha and static asset content hashes.
	/// </summary>
	public static string ExtractSection(string html, string sectionId)
	{
		var marker = $"<section id=\"{sectionId}\"";
		var start = html.IndexOf(marker, StringComparison.Ordinal);
		if (start < 0)
			throw new InvalidOperationException($"No <section id=\"{sectionId}\"> found in rendered page");

		var depth = 0;
		var position = start;
		while (position < html.Length)
		{
			var nextOpen = html.IndexOf("<section", position, StringComparison.Ordinal);
			var nextClose = html.IndexOf("</section>", position, StringComparison.Ordinal);
			if (nextClose < 0)
				break;
			if (nextOpen >= 0 && nextOpen < nextClose)
			{
				depth++;
				position = nextOpen + "<section".Length;
			}
			else
			{
				depth--;
				position = nextClose + "</section>".Length;
				if (depth == 0)
					return html[start..position];
			}
		}

		throw new InvalidOperationException($"Unbalanced <section> tags while extracting '{sectionId}'");
	}

	/// <summary>
	/// Compares against the checked-in reference under <c>ReferenceHtml/</c>.
	/// Set <c>UPDATE_SNAPSHOTS=1</c> to (re)write the reference files in the source tree instead.
	/// </summary>
	public static void MatchesReference(string name, string actualHtml)
	{
		var actual = actualHtml.Replace("\r\n", "\n");
		var referenceFile = ReferenceFile(name);

		if (Environment.GetEnvironmentVariable(UpdateEnvironmentVariable) == "1")
		{
			_ = Directory.CreateDirectory(Path.GetDirectoryName(referenceFile)!);
			File.WriteAllText(referenceFile, actual);
			return;
		}

		if (!File.Exists(referenceFile))
			Assert.Fail($"Missing reference snapshot '{name}'. Run the tests once with {UpdateEnvironmentVariable}=1 to create it, review the output and commit it.");

		var expected = File.ReadAllText(referenceFile).Replace("\r\n", "\n");
		if (string.Equals(expected, actual, StringComparison.Ordinal))
			return;

		Assert.Fail(
			$"Rendered HTML no longer matches reference snapshot '{name}'.\n" +
			$"If this change is intentional re-run with {UpdateEnvironmentVariable}=1, review the diff and commit the updated reference.\n\n" +
			BuildDiff(expected, actual));
	}

	private static string ReferenceFile(string name) =>
		Path.Combine(Paths.WorkingDirectoryRoot.FullName, "tests", "Elastic.ApiExplorer.Tests", "ReferenceHtml", $"{name}.html");

	private static string BuildDiff(string expected, string actual)
	{
		var diff = InlineDiffBuilder.Diff(expected, actual);
		var sb = new StringBuilder();
		var line = 0;
		foreach (var piece in diff.Lines)
		{
			line++;
			var prefix = piece.Type switch
			{
				ChangeType.Inserted => "+ ",
				ChangeType.Deleted => "- ",
				_ => null
			};
			if (prefix is not null)
				_ = sb.Append(prefix).Append(line).Append(": ").AppendLine(piece.Text);
		}
		return sb.ToString();
	}
}
