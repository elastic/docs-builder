// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Text;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.FrontMatter;

namespace Elastic.Markdown.Exporters;

public class LLMTextExporter : IMarkdownExporter
{
	public ValueTask StartAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public ValueTask StopAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public async ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		var source = fileContext.SourceFile.SourceFile;
		var fs = source.FileSystem;
		var llmText = fileContext.LLMText ??= ToLLMText(fileContext.BuildContext, fileContext.SourceFile.YamlFrontMatter, fileContext.Resolvers, source);

		// write to the output version of the Markdown file directly
		var outputFile = fileContext.DefaultOutputFile;
		if (outputFile.Name == "index.md")
		{
			var root = fileContext.BuildContext.DocumentationOutputDirectory;
			// Write to a file named after the parent folder
			if (outputFile.Directory!.FullName == root.FullName)
			{
				// TODO in FinishExportAsync find a way to generate llms.txt
				// e.g should it embedd all the links?
				outputFile = fs.FileInfo.New(Path.Combine(root.FullName, "llms.md"));
			}
			else
				outputFile = fs.FileInfo.New(outputFile.Directory!.FullName + ".md");
		}

		if (outputFile.Directory is { Exists: false })
			outputFile.Directory.Create();

		await fs.File.WriteAllTextAsync(outputFile.FullName, llmText, ctx);
		return true;
	}

	/// <inheritdoc />
	public ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx)
	{
		var outputDirectory = Path.Combine(outputFolder.FullName, "docs");
		var zipPath = Path.Combine(outputDirectory, "llm.zip");
		using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
		{
			var markdownFiles = Directory.GetFiles(outputDirectory, "*.md", SearchOption.AllDirectories);

			foreach (var file in markdownFiles)
			{
				var relativePath = Path.GetRelativePath(outputDirectory, file);
				_ = zip.CreateEntryFromFile(file, relativePath);
			}
		}
		return ValueTask.FromResult(true);
	}

	public static string ToLLMText(BuildContext buildContext, YamlFrontMatter? frontMatter, IParserResolvers resolvers, IFileInfo source)
	{
		var fs = source.FileSystem;
		var sb = DocumentationObjectPoolProvider.StringBuilderPool.Get();

		Read(source, fs, sb, buildContext.DocumentationSourceDirectory);
		var full = sb.ToString();
		var state = new ParserState(buildContext)
		{
			YamlFrontMatter = frontMatter,
			MarkdownSourcePath = source,
			CrossLinkResolver = resolvers.CrossLinkResolver,
			DocumentationFileLookup = resolvers.DocumentationFileLookup
		};
		DocumentationObjectPoolProvider.StringBuilderPool.Return(sb);
		var replaced = full.ReplaceSubstitutions(new ParserContext(state));
		return replaced;
	}

	private static void Read(IFileInfo source, IFileSystem fs, StringBuilder sb, IDirectoryInfo setDirectory)
	{
		var text = fs.File.ReadAllText(source.FullName).AsSpan();
		var spanStart = ":::{include}".AsSpan();
		var include = SearchValues.Create([spanStart.ToString(), ":::\n"], StringComparison.OrdinalIgnoreCase);
		int i;
		var startIndex = 0;
		while ((i = text[startIndex..].IndexOfAny(include)) >= 0)
		{
			var cursor = startIndex + i;
			var marker = text[cursor..];
			if (marker.StartsWith(spanStart))
			{
				_ = sb.Append(text.Slice(startIndex, i).TrimEnd('\n'));
				var relativeFileStart = marker.IndexOf('}') + 1;
				var relativeFileEnd = marker.IndexOf('\n');
				var relativeFile = marker[relativeFileStart..relativeFileEnd].Trim();
				var includePath = Path.GetFullPath(Path.Combine(source.Directory!.FullName, relativeFile.ToString()));
				var includeSource = fs.FileInfo.New(includePath);
				if (relativeFile.StartsWith('/'))
				{
					includePath = Path.Combine(setDirectory.FullName, relativeFile.TrimStart('/').ToString());
					includeSource = fs.FileInfo.New(includePath);
				}

				if (includeSource.Extension == "md" && includePath.Contains("_snippets"))
					Read(includeSource, fs, sb, setDirectory);
				startIndex = cursor + relativeFileEnd;
				startIndex = Math.Min(text.Length, startIndex);
			}
			else
			{
				startIndex += i + 4;
				startIndex = Math.Min(text.Length, startIndex);
			}
		}

		_ = sb.Append(text[startIndex..]);
	}
}
