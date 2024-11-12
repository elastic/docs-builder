// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Data.SqlTypes;
using System.IO.Abstractions;
using Cysharp.IO;
using Elastic.Markdown.Diagnostics;
using Markdig.Helpers;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Elastic.Markdown.IO;

public class ConfigurationFile : DocumentationFile
{
	private readonly IFileInfo _sourceFile;
	private readonly BuildContext _context;
	public string? Project { get; }
	public string[] Exclude { get; } = [];

	public IReadOnlyCollection<ITocItem> TableOfContents { get; } = [];

	public ConfigurationFile(IFileInfo sourceFile, IDirectoryInfo rootPath, BuildContext context)
		: base(sourceFile, rootPath)
	{
		_sourceFile = sourceFile;
		_context = context;
		if (!sourceFile.Exists)
		{
			Project = "unknown";
			TableOfContents = [];
			context.EmitWarning(sourceFile, "No configuration file found");
			return;
		}

		// Load the stream
		var yaml = new YamlStream();
		var textReader = sourceFile.FileSystem.File.OpenText(sourceFile.FullName);
		yaml.Load(textReader);

		if (yaml.Documents.Count == 0)
			context.EmitWarning(sourceFile, "empty configuration");

		// Examine the stream
		var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

		foreach (var entry in mapping.Children)
		{
			var key = ((YamlScalarNode)entry.Key).Value;
			switch (key)
			{
				case "project":
					Project = ReadString(entry);
					break;
				case "exclude":
					Exclude = ReadStringArray(entry) ?? [];
					break;
				case "toc":
					var entries = ReadChildren(entry);

					TableOfContents = entries;
					break;
				default:
					EmitWarning($"{key} is not a known configuration", entry.Key);
					break;
			}
		}

	}

	private List<ITocItem> ReadChildren(KeyValuePair<YamlNode, YamlNode> entry)
	{
		var entries = new List<ITocItem>();
		if (entry.Value is not YamlSequenceNode sequence)
		{
			var key = ((YamlScalarNode)entry.Key).Value;
			EmitWarning($"'{key}' is not an array");
			return entries;
		}

		foreach (var tocEntry in sequence.Children.OfType<YamlMappingNode>())
		{
			var tocItem = ReadChild(tocEntry);
			if (tocItem is not null)
				entries.Add(tocItem);
		}

		return entries;
	}

	private ITocItem? ReadChild(YamlMappingNode tocEntry)
	{
		string? file = null;
		string? folder = null;
		IReadOnlyCollection<ITocItem>? children = null;
		foreach (var entry in tocEntry.Children)
		{
			var key = ((YamlScalarNode)entry.Key).Value;
			switch (key)
			{
				case "file":
					file = ReadString(entry);
					break;
				case "folder":
					folder = ReadString(entry);
					break;
				case "children":
					children = ReadChildren(entry);
					break;
			}
		}

		if (file is not null)
			return new TocFile(file, children);
		if (folder is not null)
			return new TocFile(folder, children);

		return null;
	}

	private string? ReadString(KeyValuePair<YamlNode, YamlNode> entry)
	{
		if (entry.Value is YamlScalarNode scalar)
			return scalar.Value;

		var key = ((YamlScalarNode)entry.Key).Value;

		EmitError($"'{key}' is not a string", entry.Key);
		return null;
	}
	private string[]? ReadStringArray(KeyValuePair<YamlNode, YamlNode> entry)
	{
		var values = new List<string>();
		if (entry.Value is YamlSequenceNode sequence)
		{
			foreach (var entryValue in sequence.Children.OfType<YamlScalarNode>())
			{
				if (entryValue.Value is not null)
					values.Add(entryValue.Value);
			}

		}
		return values.ToArray();
	}

	public void EmitError(string message, YamlNode? node) => EmitError(message, node?.Start, node?.End);

	public void EmitWarning(string message, YamlNode? node) => EmitError(message, node?.Start, node?.End);

	public void EmitError(string message, Mark? start = null, Mark? end = null)
	{
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = _sourceFile.FullName,
			Message = message,
			Line = start.HasValue ? (int)start.Value.Line : null,
			Column = start.HasValue ? (int)start.Value.Column : null,
			Length = start.HasValue && end.HasValue ? (int)start.Value.Column - (int)end.Value.Column : null
		};
		_context.Collector.Channel.Write(d);
	}

	public void EmitWarning(string message, Mark? start = null, Mark? end = null)
	{
		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = _sourceFile.FullName,
			Message = message,
			Line = start.HasValue ? (int)start.Value.Line : null,
			Column = start.HasValue ? (int)start.Value.Column : null,
			Length = start.HasValue && end.HasValue ? (int)start.Value.Column - (int)end.Value.Column : null
		};
		_context.Collector.Channel.Write(d);
	}
}

public interface ITocItem;

public record TocFile(string Path, IReadOnlyCollection<ITocItem>? Children) : ITocItem;

public record TocFolder(string Path, IReadOnlyCollection<ITocItem>? Children) : ITocItem;


/*
exclude:
  - notes.md
  - '**ignore.md'
toc:
- file: index.md
- file: config.md
- file: search.md
children:
- file: search-part2.md
- folder: search
- folder: my-folder1
exclude:
- '_*.md'
- folder: my-folder2
children:
- file: subpath/file.md
- file: file.md
- pattern: *.md
- folder: sub/folder
*/
