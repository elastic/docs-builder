// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using DotNet.Globbing;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Extensions;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.IO.State;
using YamlDotNet.RepresentationModel;

namespace Elastic.Markdown.IO.Configuration;

public record ConfigurationFile : DocumentationFile
{
	private readonly IDirectoryInfo _rootPath;
	private readonly BuildContext _context;
	private readonly int _depth;
	public string? Project { get; }
	public Glob[] Exclude { get; } = [];

	public string[] CrossLinkRepositories { get; } = [];

	public EnabledExtensions Extensions { get; } = new([]);
	public IReadOnlyCollection<IDocsBuilderExtension> EnabledExtensions { get; } = [];

	public IReadOnlyCollection<ITocItem> TableOfContents { get; } = [];

	public Dictionary<string, LinkRedirect>? Redirects { get; }

	public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
	public HashSet<string> ImplicitFolders { get; } = new(StringComparer.OrdinalIgnoreCase);
	public Glob[] Globs { get; } = [];

	private readonly Dictionary<string, string> _substitutions = new(StringComparer.OrdinalIgnoreCase);
	public IReadOnlyDictionary<string, string> Substitutions => _substitutions;

	private readonly Dictionary<string, bool> _features = new(StringComparer.OrdinalIgnoreCase);
	private FeatureFlags? _featureFlags;
	public FeatureFlags Features => _featureFlags ??= new FeatureFlags(_features);

	public ConfigurationFile(IFileInfo sourceFile, IDirectoryInfo rootPath, BuildContext context, int depth = 0, string parentPath = "")
		: base(sourceFile, rootPath)
	{
		_rootPath = rootPath;
		_context = context;
		_depth = depth;
		if (!sourceFile.Exists)
		{
			Project = "unknown";
			context.EmitWarning(sourceFile, "No configuration file found");
			return;
		}

		var redirectFileName = sourceFile.Name.StartsWith('_') ? "_redirects.yml" : "redirects.yml";
		var redirectFileInfo = sourceFile.FileSystem.FileInfo.New(Path.Combine(sourceFile.Directory!.FullName, redirectFileName));
		var redirectFile = new RedirectFile(redirectFileInfo, _context);
		Redirects = redirectFile.Redirects;

		var reader = new YamlStreamReader(sourceFile, _context);
		try
		{
			foreach (var entry in reader.Read())
			{
				switch (entry.Key)
				{
					case "project":
						Project = reader.ReadString(entry.Entry);
						break;
					case "exclude":
						Exclude = [.. YamlStreamReader.ReadStringArray(entry.Entry).Select(Glob.Parse)];
						break;
					case "cross_links":
						CrossLinkRepositories = [.. YamlStreamReader.ReadStringArray(entry.Entry)];
						break;
					case "extensions":
						Extensions = new([.. YamlStreamReader.ReadStringArray(entry.Entry)]);
						EnabledExtensions = InstantiateExtensions();
						break;
					case "subs":
						_substitutions = reader.ReadDictionary(entry.Entry);
						break;
					case "toc":
						if (depth > 1)
						{
							reader.EmitError($"toc.yml files may only be linked from docset.yml", entry.Key);
							break;
						}

						var entries = ReadChildren(reader, entry.Entry, parentPath);

						TableOfContents = entries;
						break;
					case "features":
						_features = reader.ReadDictionary(entry.Entry).ToDictionary(k => k.Key, v => bool.Parse(v.Value), StringComparer.OrdinalIgnoreCase);
						break;
					case "external_hosts":
						reader.EmitWarning($"{entry.Key} has been deprecated and will be removed", entry.Key);
						break;
					default:
						reader.EmitWarning($"{entry.Key} is not a known configuration", entry.Key);
						break;
				}
			}
		}
		catch (Exception e)
		{
			reader.EmitError("Could not load docset.yml", e);
			throw;
		}

		Globs = [.. ImplicitFolders.Select(f => Glob.Parse($"{f}/*.md"))];
	}

	private IReadOnlyCollection<IDocsBuilderExtension> InstantiateExtensions()
	{
		var list = new List<IDocsBuilderExtension>();
		foreach (var extension in Extensions.Enabled)
		{
			switch (extension.ToLowerInvariant())
			{
				case "detection-rules":
					list.Add(new DetectionRulesDocsBuilderExtension(_context));
					continue;
			}
		}

		return list.AsReadOnly();
	}


	private List<ITocItem> ReadChildren(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, string parentPath)
	{
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

		return entries;
	}

	private IEnumerable<ITocItem>? ReadChild(YamlStreamReader reader, YamlMappingNode tocEntry, string parentPath)
	{
		string? file = null;
		string? folder = null;
		string? detectionRules = null;
		ConfigurationFile? toc = null;
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
					toc = ReadNestedToc(reader, entry, out fileFound);
					break;
				case "hidden":
				case "file":
					hiddenFile = key == "hidden";
					file = ReadFile(reader, entry, parentPath, out fileFound);
					break;
				case "folder":
					folder = ReadFolder(reader, entry, parentPath, out folderFound);
					parentPath += $"/{folder}";
					break;
				case "detection_rules":
					if (Extensions.IsDetectionRulesEnabled)
					{
						detectionRules = ReadDetectionRules(reader, entry, parentPath, out detectionRulesFound);
						parentPath += $"/{folder}";
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

			return [new FolderReference($"{parentPath}".TrimStart('/'), folderFound, toc.TableOfContents)];
		}

		if (file is not null)
			return [new FileReference($"{parentPath}/{file}".TrimStart('/'), fileFound, hiddenFile, children ?? [])];

		if (folder is not null)
		{
			if (children is null)
				_ = ImplicitFolders.Add(parentPath.TrimStart('/'));

			return [new FolderReference($"{parentPath}".TrimStart('/'), folderFound, children ?? [])];
		}

		if (detectionRules is not null)
		{
			if (children is not null)
				reader.EmitError($"'detection_rules' is not allowed to have 'children'", tocEntry);

			if (!detectionRulesFound)
			{
				reader.EmitError($"'detection_rules' folder {parentPath} is not found, skipping'", tocEntry);
				return [];
			}

			var extension = EnabledExtensions.OfType<DetectionRulesDocsBuilderExtension>().First();
			return extension.CreateTableOfContentItems(parentPath, detectionRulesFound, detectionRules, Files);
		}

		return null;
	}

	private string? ReadFolder(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, string parentPath, out bool found)
	{
		found = false;
		var folder = reader.ReadString(entry);
		if (folder is not null)
		{
			var path = Path.Combine(_rootPath.FullName, parentPath.TrimStart('/'), folder);
			if (!_context.ReadFileSystem.DirectoryInfo.New(path).Exists)
				reader.EmitError($"Directory '{path}' does not exist", entry.Key);
			else
				found = true;
		}

		return folder;
	}

	private string? ReadDetectionRules(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, string parentPath, out bool found)
	{
		found = false;
		var folder = reader.ReadString(entry);
		if (folder is not null)
		{
			var path = Path.Combine(_rootPath.FullName, parentPath.TrimStart('/'), folder);
			if (!_context.ReadFileSystem.DirectoryInfo.New(path).Exists)
				reader.EmitError($"Directory '{path}' does not exist", entry.Key);
			else
				found = true;
		}

		return folder;
	}

	private string? ReadFile(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, string parentPath, out bool found)
	{
		found = false;
		var file = reader.ReadString(entry);
		if (file is null)
			return null;

		var path = Path.Combine(_rootPath.FullName, parentPath.TrimStart('/'), file);
		if (!_context.ReadFileSystem.FileInfo.New(path).Exists)
			reader.EmitError($"File '{path}' does not exist", entry.Key);
		else
			found = true;
		_ = Files.Add((parentPath + "/" + file).TrimStart('/'));

		return file;
	}

	private ConfigurationFile? ReadNestedToc(YamlStreamReader reader, KeyValuePair<YamlNode, YamlNode> entry, out bool found)
	{
		found = false;
		var tocPath = reader.ReadString(entry);
		if (tocPath is null)
		{
			reader.EmitError($"Empty toc: reference", entry.Key);
			return null;
		}

		var rootPath = _context.ReadFileSystem.DirectoryInfo.New(Path.Combine(_rootPath.FullName, tocPath));
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

		var nestedConfiguration = new ConfigurationFile(source, _rootPath, _context, _depth + 1, tocPath);
		return nestedConfiguration;
	}
}
