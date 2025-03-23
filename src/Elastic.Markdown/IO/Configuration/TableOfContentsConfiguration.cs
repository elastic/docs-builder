// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Elastic.Markdown.Extensions.DetectionRules;
using YamlDotNet.RepresentationModel;

namespace Elastic.Markdown.IO.Configuration;

public interface ITableOfContentsScope
{
	IDirectoryInfo ScopeDirectory { get; }
}

public record TableOfContentsConfiguration : ITableOfContentsScope
{
	private readonly BuildContext _context;
	private readonly int _maxTocDepth;
	private readonly int _depth;
	private readonly string _parentPath;
	private readonly IDirectoryInfo _rootPath;
	private readonly ConfigurationFile _configuration;

	public Uri Source { get; }

	public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);

	public IReadOnlyCollection<ITocItem> TableOfContents { get; private set; } = [];

	public IFileInfo DefinitionFile { get; }
	public IDirectoryInfo ScopeDirectory { get; }

	public TableOfContentsConfiguration(
		ConfigurationFile configuration,
		IFileInfo definitionFile,
		IDirectoryInfo scope,
		BuildContext context,
		int depth,
		string parentPath)
	{
		_configuration = configuration;
		DefinitionFile = definitionFile;
		ScopeDirectory = scope;
		_maxTocDepth = configuration.MaxTocDepth;
		_rootPath = context.DocumentationSourceDirectory;
		_context = context;
		_depth = depth;
		_parentPath = parentPath;

		var tocPath = scope.FullName;
		var relativePath = Path.GetRelativePath(context.DocumentationSourceDirectory.FullName, tocPath);
		var moniker = $"{context.Git.RepositoryName}://{relativePath}";
		Source = new Uri(moniker);

		TableOfContents = ReadChildren();

	}

	private IReadOnlyCollection<ITocItem> ReadChildren()
	{
		if (!DefinitionFile.Exists)
			return [];
		var reader = new YamlStreamReader(DefinitionFile, _context.Collector);
		foreach (var entry in reader.Read())
		{
			switch (entry.Key)
			{
				case "toc":
					var children = ReadChildren(reader, entry.Entry);
					var tocEntries = TableOfContents.OfType<TocReference>().ToArray();
					if (!_configuration.DevelopmentDocs && !_configuration.IsNarrativeDocs && tocEntries.Length > 0 && TableOfContents.Count != tocEntries.Length)
						reader.EmitError("toc links to other toc sections it may only contain other toc references", entry.Key);
					return children;
			}
		}


		return [];
	}

	private IReadOnlyCollection<ITocItem> ReadChildren(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, string? parentPath = null)
	{
		parentPath ??= _parentPath;
		if (_depth > _maxTocDepth)
		{
			reader.EmitError($"toc.yml files may only be linked from docset.yml", entry.Key);
			return [];
		}

		var entries = new List<ITocItem>();
		if (entry.Value is not YamlSequenceNode sequence)
		{
			if (entry.Key is YamlScalarNode scalarKey)
			{
				var key = scalarKey.Value;
				reader.EmitWarning($"'{key}' is not an array");
			}
			else
				reader.EmitWarning($"'{entry.Key}' is not an array");

			return entries;
		}

		entries.AddRange(
			sequence.Children.OfType<YamlMappingNode>()
				.SelectMany(tocEntry => ReadChild(reader, tocEntry, parentPath) ?? [])
		);
		TableOfContents = entries;
		return entries;
	}

	private IEnumerable<ITocItem>? ReadChild(YamlStreamReader reader, YamlMappingNode tocEntry, string parentPath)
	{
		string? file = null;
		string? folder = null;
		string[]? detectionRules = null;
		TableOfContentsConfiguration? toc = null;
		var fileFound = false;
		var folderFound = false;
		var detectionRulesFound = false;
		var hiddenFile = false;
		IReadOnlyCollection<ITocItem>? children = null;
		foreach (var entry in tocEntry.Children)
		{
			var key = ((YamlScalarNode)entry.Key).Value;
			switch (key)
			{
				case "toc":
					toc = ReadNestedToc(reader, entry, parentPath, out fileFound);
					break;
				case "hidden":
				case "file":
					hiddenFile = key == "hidden";
					file = ReadFile(reader, entry, parentPath, out fileFound);
					break;
				case "folder":
					folder = ReadFolder(reader, entry, parentPath, out folderFound);
					parentPath += $"{Path.DirectorySeparatorChar}{folder}";
					break;
				case "detection_rules":
					if (_configuration.Extensions.IsDetectionRulesEnabled)
					{
						detectionRules = ReadDetectionRules(reader, entry, parentPath, out detectionRulesFound);
						parentPath += $"{Path.DirectorySeparatorChar}{folder}";
					}
					break;
				case "children":
					children = ReadChildren(reader, entry, parentPath);
					break;
			}
		}

		if (toc is not null)
		{
			foreach (var f in toc.Files)
				_ = Files.Add(f);

			return [new TocReference(toc.Source, toc, $"{parentPath}".TrimStart(Path.DirectorySeparatorChar), folderFound, toc.TableOfContents)];
		}

		if (file is not null)
		{
			if (detectionRules is not null)
			{
				if (children is not null)
					reader.EmitError($"'detection_rules' is not allowed to have 'children'", tocEntry);

				if (!detectionRulesFound)
				{
					reader.EmitError($"'detection_rules' folder {parentPath} is not found, skipping'", tocEntry);
					children = [];
				}
				else
				{
					var extension = _configuration.EnabledExtensions.OfType<DetectionRulesDocsBuilderExtension>().First();
					children = extension.CreateTableOfContentItems(_configuration, parentPath, detectionRules, Files);
				}
			}
			return [new FileReference(this, $"{parentPath}{Path.DirectorySeparatorChar}{file}".TrimStart(Path.DirectorySeparatorChar), fileFound, hiddenFile, children ?? [])];
		}

		if (folder is not null)
		{
			if (children is null)
				_ = _configuration.ImplicitFolders.Add(parentPath.TrimStart(Path.DirectorySeparatorChar));

			return [new FolderReference(this, $"{parentPath}".TrimStart(Path.DirectorySeparatorChar), folderFound, children ?? [])];
		}

		return null;
	}

	private string? ReadFolder(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, string parentPath, out bool found)
	{
		found = false;
		var folder = reader.ReadString(entry);
		if (folder is not null)
		{
			var path = Path.Combine(_rootPath.FullName, parentPath.TrimStart(Path.DirectorySeparatorChar), folder);
			if (!_context.ReadFileSystem.DirectoryInfo.New(path).Exists)
				reader.EmitError($"Directory '{path}' does not exist", entry.Key);
			else
				found = true;
		}

		return folder;
	}

	private string[]? ReadDetectionRules(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, string parentPath, out bool found)
	{
		found = false;
		var folders = YamlStreamReader.ReadStringArray(entry);
		foreach (var folder in folders)
		{
			if (string.IsNullOrWhiteSpace(folder))
				continue;

			var path = Path.Combine(_rootPath.FullName, parentPath.TrimStart(Path.DirectorySeparatorChar), folder);
			if (!_context.ReadFileSystem.DirectoryInfo.New(path).Exists)
				reader.EmitError($"Directory '{path}' does not exist", entry.Key);
			else
				found = true;

		}
		return folders.Length == 0 ? null : folders;
	}

	private string? ReadFile(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, string parentPath, out bool found)
	{
		found = false;
		var file = reader.ReadString(entry);
		if (file is null)
			return null;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			file = file.Replace('/', Path.DirectorySeparatorChar);

		var path = Path.Combine(_rootPath.FullName, parentPath.TrimStart(Path.DirectorySeparatorChar), file);
		if (!_context.ReadFileSystem.FileInfo.New(path).Exists)
			reader.EmitError($"File '{path}' does not exist", entry.Key);
		else
			found = true;
		_ = Files.Add(Path.Combine(parentPath, file).TrimStart(Path.DirectorySeparatorChar));

		return file;
	}

	private TableOfContentsConfiguration? ReadNestedToc(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, string parentPath, out bool found)
	{
		found = false;
		var tocPath = reader.ReadString(entry);
		if (tocPath is null)
		{
			reader.EmitError($"Empty toc: reference", entry.Key);
			return null;
		}
		var fullTocPath = Path.Combine(parentPath, tocPath);

		var rootPath = _context.ReadFileSystem.DirectoryInfo.New(Path.Combine(_rootPath.FullName, fullTocPath));
		var path = Path.Combine(rootPath.FullName, "toc.yml");
		var source = _context.ReadFileSystem.FileInfo.New(path);

		var errorMessage = $"Nested toc: '{source.Directory}' directory has no toc.yml or _toc.yml file";

		if (!source.Exists)
		{
			path = Path.Combine(rootPath.FullName, "_toc.yml");
			source = _context.ReadFileSystem.FileInfo.New(path);
		}

		if (!source.Exists)
			reader.EmitError(errorMessage, entry.Key);
		else
			found = true;

		if (!found)
			return null;

		var tocYamlReader = new YamlStreamReader(source, _context.Collector);
		foreach (var kv in tocYamlReader.Read())
		{
			switch (kv.Key)
			{
				case "toc":
					var nestedConfiguration = new TableOfContentsConfiguration(_configuration, source, source.Directory!, _context, _depth + 1, fullTocPath);
					_ = nestedConfiguration.ReadChildren(reader, kv.Entry);
					return nestedConfiguration;
			}
		}
		return null;
	}
}
