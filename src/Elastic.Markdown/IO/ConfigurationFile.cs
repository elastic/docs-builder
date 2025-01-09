// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using DotNet.Globbing;
using Elastic.Markdown.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Elastic.Markdown.IO;

public record ConfigurationFile : DocumentationFile
{
	private readonly IFileInfo _sourceFile;
	private readonly IDirectoryInfo _rootPath;
	private readonly BuildContext _context;
	public string? Project { get; }
	public Glob[] Exclude { get; } = [];

	public IReadOnlyCollection<ITocItem> TableOfContents { get; } = [];

	public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
	public HashSet<string> ImplicitFolders { get; } = new(StringComparer.OrdinalIgnoreCase);
	public Glob[] Globs { get; } = [];
	public HashSet<string> ExternalLinkHosts { get; } = new(StringComparer.OrdinalIgnoreCase) { "elastic.co", "github.com", "localhost", };

	private readonly Dictionary<string, string> _substitutions = new(StringComparer.OrdinalIgnoreCase);
	public IReadOnlyDictionary<string, string> Substitutions => _substitutions;

	public ConfigurationFile(IFileInfo sourceFile, IDirectoryInfo rootPath, BuildContext context)
		: base(sourceFile, rootPath)
	{
		_sourceFile = sourceFile;
		_rootPath = rootPath;
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
		{
			context.EmitWarning(sourceFile, "empty configuration");
			return;
		}

		try
		{
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
						Exclude = ReadStringArray(entry)
							.Select(Glob.Parse)
							.ToArray();
						break;
					case "subs":
						_substitutions = ReadDictionary(entry);
						break;
					case "external_hosts":
						var hosts = ReadStringArray(entry)
							.ToArray();
						foreach (var host in hosts)
							ExternalLinkHosts.Add(host);
						break;
					case "toc":
						var entries = ReadChildren(entry, string.Empty);

						TableOfContents = entries;
						break;
					default:
						EmitWarning($"{key} is not a known configuration", entry.Key);
						break;
				}
			}
		}
		catch (Exception e)
		{
			EmitError("Could not load docset.yml", e);
		}

		Globs = ImplicitFolders.Select(f => Glob.Parse($"{f}/*.md")).ToArray();
	}

	private List<ITocItem> ReadChildren(KeyValuePair<YamlNode, YamlNode> entry, string parentPath)
	{
		var entries = new List<ITocItem>();
		if (entry.Value is not YamlSequenceNode sequence)
		{
			if (entry.Key is YamlScalarNode scalarKey)
			{
				var key = scalarKey.Value;
				EmitWarning($"'{key}' is not an array");
			}
			else
				EmitWarning($"'{entry.Key}' is not an array");

			return entries;
		}

		foreach (var tocEntry in sequence.Children.OfType<YamlMappingNode>())
		{
			var tocItem = ReadChild(tocEntry, parentPath);
			if (tocItem is not null)
				entries.Add(tocItem);
		}

		return entries;
	}

	private ITocItem? ReadChild(YamlMappingNode tocEntry, string parentPath)
	{
		string? file = null;
		string? folder = null;
		var fileFound = false;
		var folderFound = false;
		IReadOnlyCollection<ITocItem>? children = null;
		foreach (var entry in tocEntry.Children)
		{
			var key = ((YamlScalarNode)entry.Key).Value;
			switch (key)
			{
				case "file":
					file = ReadFile(entry, parentPath, out fileFound);
					break;
				case "folder":
					folder = ReadFolder(entry, parentPath, out folderFound);
					parentPath += $"/{folder}";
					break;
				case "children":
					children = ReadChildren(entry, parentPath);
					break;
			}
		}

		if (file is not null)
			return new TocFile($"{parentPath}/{file}".TrimStart('/'), fileFound, children ?? []);

		if (folder is not null)
		{
			if (children is null)
				ImplicitFolders.Add(parentPath.TrimStart('/'));

			return new TocFolder($"{parentPath}".TrimStart('/'), folderFound, children ?? []);
		}

		return null;
	}

	private Dictionary<string, string> ReadDictionary(KeyValuePair<YamlNode, YamlNode> entry)
	{
		var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (entry.Value is not YamlMappingNode mapping)
		{
			if (entry.Key is YamlScalarNode scalarKey)
			{
				var key = scalarKey.Value;
				EmitWarning($"'{key}' is not a dictionary");
			}
			else
				EmitWarning($"'{entry.Key}' is not a dictionary");

			return dictionary;
		}

		foreach (var entryValue in mapping.Children)
		{
			if (entryValue.Key is not YamlScalarNode scalar || scalar.Value is null)
				continue;
			var key = scalar.Value;
			var value = ReadString(entryValue);
			if (value is not null)
				dictionary.Add(key, value);
		}

		return dictionary;
	}

	private string? ReadFolder(KeyValuePair<YamlNode, YamlNode> entry, string parentPath, out bool found)
	{
		found = false;
		var folder = ReadString(entry);
		if (folder is not null)
		{
			var path = Path.Combine(_rootPath.FullName, parentPath.TrimStart('/'), folder);
			if (!_context.ReadFileSystem.DirectoryInfo.New(path).Exists)
				EmitError($"Directory '{path}' does not exist", entry.Key);
			else
				found = true;
		}

		return folder;
	}

	private string? ReadFile(KeyValuePair<YamlNode, YamlNode> entry, string parentPath, out bool found)
	{
		found = false;
		var file = ReadString(entry);
		if (file is null)
			return null;

		var path = Path.Combine(_rootPath.FullName, parentPath.TrimStart('/'), file);
		if (!_context.ReadFileSystem.FileInfo.New(path).Exists)
			EmitError($"File '{path}' does not exist", entry.Key);
		else
			found = true;
		Files.Add((parentPath + "/" + file).TrimStart('/'));

		return file;
	}

	private string? ReadString(KeyValuePair<YamlNode, YamlNode> entry)
	{
		if (entry.Value is YamlScalarNode scalar)
			return scalar.Value;

		if (entry.Key is YamlScalarNode scalarKey)
		{
			var key = scalarKey.Value;
			EmitError($"'{key}' is not a string", entry.Key);
			return null;
		}

		EmitError($"'{entry.Key}' is not a string", entry.Key);
		return null;
	}

	private string[] ReadStringArray(KeyValuePair<YamlNode, YamlNode> entry)
	{
		var values = new List<string>();
		if (entry.Value is not YamlSequenceNode sequence)
			return values.ToArray();

		foreach (var entryValue in sequence.Children.OfType<YamlScalarNode>())
		{
			if (entryValue.Value is not null)
				values.Add(entryValue.Value);
		}

		return values.ToArray();
	}

	private void EmitError(string message, YamlNode? node) =>
		EmitError(message, node?.Start, node?.End, (node as YamlScalarNode)?.Value?.Length);

	private void EmitWarning(string message, YamlNode? node) =>
		EmitWarning(message, node?.Start, node?.End, (node as YamlScalarNode)?.Value?.Length);

	private void EmitError(string message, Exception e) =>
		_context.Collector.EmitError(_sourceFile.FullName, message, e);

	private void EmitError(string message, Mark? start = null, Mark? end = null, int? length = null)
	{
		length ??= start.HasValue && end.HasValue ? (int)start.Value.Column - (int)end.Value.Column : null;
		var d = new Diagnostic
		{
			Severity = Severity.Error,
			File = _sourceFile.FullName,
			Message = message,
			Line = start.HasValue ? (int)start.Value.Line : null,
			Column = start.HasValue ? (int)start.Value.Column : null,
			Length = length
		};
		_context.Collector.Channel.Write(d);
	}

	private void EmitWarning(string message, Mark? start = null, Mark? end = null, int? length = null)
	{
		length ??= start.HasValue && end.HasValue ? (int)start.Value.Column - (int)end.Value.Column : null;
		var d = new Diagnostic
		{
			Severity = Severity.Warning,
			File = _sourceFile.FullName,
			Message = message,
			Line = start.HasValue ? (int)start.Value.Line : null,
			Column = start.HasValue ? (int)start.Value.Column : null,
			Length = length
		};
		_context.Collector.Channel.Write(d);
	}
}
