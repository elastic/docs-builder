// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.FrontMatter;
using Markdig.Syntax;

namespace Elastic.Markdown.Exporters;

public class LLMTextExporter : IMarkdownExporter
{
	public ValueTask StartAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public ValueTask StopAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public async ValueTask<bool> ExportAsync(MarkdownExportContext context, Cancel ctx)
	{
		var source = context.File.SourceFile;
		var fs = source.FileSystem;
		var llmText = context.LLMText ??= ToLLMText(context.BuildContext, context.File.YamlFrontMatter, context.Resolvers, source);

		var newFile = fs.FileInfo.New(Path.ChangeExtension(source.FullName, ".md"));
		await fs.File.WriteAllTextAsync(newFile.FullName, llmText, ctx);
		return true;
	}

	public static string ToLLMText(BuildContext buildContext, YamlFrontMatter? frontMatter, IParserResolvers resolvers, IFileInfo source)
	{
		var fs = source.FileSystem;
		var sb = DocumentationObjectPoolProvider.StringBuilderPool.Get();

		Read(source, fs, sb);
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

	private static void Read(IFileInfo source, IFileSystem fs, StringBuilder sb, int depth = 0)
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
				Read(includeSource, fs, sb, depth + 1);
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

