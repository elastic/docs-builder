// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Toc.CliReference;
using Elastic.Documentation.Navigation;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;

namespace Elastic.Markdown.Extensions.CliReference;

internal sealed record CliEntityInfo(
	ArghCliSchema Schema,
	object Entity, // ArghCliSchema | CliNamespaceSchema | CliCommandSchema
	IFileInfo? SupplementalDoc
);

public class CliReferenceDocsBuilderExtension(BuildContext build) : IDocsBuilderExtension
{
	private BuildContext Build { get; } = build;

	private Dictionary<string, CliEntityInfo>? _syntheticFiles;
	private List<IFileInfo>? _syntheticFileInfos;

	// Must be called before CreateMarkdownFile or CreateDocumentationFile can match anything.
	// ScanDocumentationFiles calls this; CreateMarkdownFile also triggers it because the main
	// directory scan runs before ScanDocumentationFiles, so index.md files are encountered first.
	private void EnsureSyntheticFilesBuilt()
	{
		if (_syntheticFiles is not null)
			return;
		_syntheticFiles = [];
		_syntheticFileInfos = BuildSyntheticFiles();
	}

	public IDocumentationFileExporter? FileExporter => null;

	public DocumentationFile? CreateDocumentationFile(IFileInfo file, MarkdownParser markdownParser)
	{
		EnsureSyntheticFilesBuilt();
		if (!_syntheticFiles!.TryGetValue(file.FullName, out var info))
			return null;

		return info.Entity switch
		{
			ArghCliSchema schema => new CliRootFile(file, Build.DocumentationSourceDirectory, markdownParser, Build, schema, info.SupplementalDoc),
			CliNamespaceSchema ns => new CliNamespaceFile(file, Build.DocumentationSourceDirectory, markdownParser, Build, ns, info.SupplementalDoc),
			CliCommandSchema cmd => new CliCommandFile(file, Build.DocumentationSourceDirectory, markdownParser, Build, cmd, info.SupplementalDoc),
			_ => null
		};
	}

	public MarkdownFile? CreateMarkdownFile(IFileInfo file, IDirectoryInfo sourceDirectory, MarkdownParser markdownParser)
	{
		// Physical CLI supplemental docs (index.md for namespaces, cmd-*.md for commands) live at the same
		// path as their synthetic CLI page. EnsureSyntheticFilesBuilt() is needed here because
		// CreateMarkdownFile is called during the main directory scan, before ScanDocumentationFiles runs.
		var name = file.Name;
		if (name != "index.md" && !name.StartsWith("cmd-", StringComparison.OrdinalIgnoreCase))
			return null;
		EnsureSyntheticFilesBuilt();
		var fullPath = Path.GetFullPath(file.FullName);
		if (!_syntheticFiles!.TryGetValue(fullPath, out var info))
			return null;
		return info.Entity switch
		{
			ArghCliSchema schema => new CliRootFile(file, Build.DocumentationSourceDirectory, markdownParser, Build, schema, info.SupplementalDoc),
			CliNamespaceSchema ns => new CliNamespaceFile(file, Build.DocumentationSourceDirectory, markdownParser, Build, ns, info.SupplementalDoc),
			CliCommandSchema cmd => new CliCommandFile(file, Build.DocumentationSourceDirectory, markdownParser, Build, cmd, info.SupplementalDoc),
			_ => null
		};
	}

	public void VisitNavigation(INavigationItem navigation, IDocumentationFile model) { }

	public bool TryGetDocumentationFileBySlug(DocumentationSet documentationSet, string slug, out DocumentationFile? documentationFile)
	{
		documentationFile = null;
		return false;
	}

	public IReadOnlyCollection<(IFileInfo, DocumentationFile)> ScanDocumentationFiles(Func<IFileInfo, IDirectoryInfo, DocumentationFile> defaultFileHandling)
	{
		EnsureSyntheticFilesBuilt();
		if (_syntheticFileInfos is not { Count: > 0 })
			return [];

		var results = new List<(IFileInfo, DocumentationFile)>();
		foreach (var fileInfo in _syntheticFileInfos)
		{
			// When a supplemental index.md physically exists at the synthetic path (e.g. changelog/index.md),
			// skip it here — the factory's directory scan will find the real file and call CreateMarkdownFile,
			// which picks up the CliNamespaceFile from _syntheticFiles. Registering both would cause duplicate keys.
			if (fileInfo.Exists)
				continue;

			// defaultFileHandling calls extension.CreateDocumentationFile(file, markdownParser)
			// which routes back to our CreateDocumentationFile above — now with the MarkdownParser available
			var doc = defaultFileHandling(fileInfo, Build.DocumentationSourceDirectory);
			results.Add((fileInfo, doc));
		}
		return results;
	}

	private List<IFileInfo> BuildSyntheticFiles()
	{
		var cliRefs = FindCliReferenceRefs(Build.ConfigurationYaml.TableOfContents);
		var fileInfos = new List<IFileInfo>();

		foreach (var cliRef in cliRefs)
		{
			var schemaFileInfo = Build.ReadFileSystem.FileInfo.New(
				Build.ReadFileSystem.Path.Join(Build.DocumentationSourceDirectory.FullName, cliRef.SchemaPath));

			if (!schemaFileInfo.Exists)
				continue;

			ArghCliSchema schema;
			try
			{
				schema = ArghCliSchema.Load(schemaFileInfo);
			}
			catch (Exception ex)
			{
				Build.Collector.EmitError(schemaFileInfo, $"Failed to load CLI schema: {ex.Message}");
				continue;
			}

			var virtualRoot = cliRef.PathRelativeToDocumentationSet;
			var supplementalDirPath = cliRef.SupplementalFolder is not null
				? Build.ReadFileSystem.Path.Join(Build.DocumentationSourceDirectory.FullName, cliRef.SupplementalFolder)
				: null;

			var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// Root page
			var rootSupplemental = FindSupplemental(supplementalDirPath, [], isNamespace: true, matched);
			var rootSyntheticPath = SyntheticPath(Build.DocumentationSourceDirectory.FullName, virtualRoot, [], isNamespace: true);
			var rootFileInfo = Build.ReadFileSystem.FileInfo.New(rootSyntheticPath);
			_syntheticFiles![rootSyntheticPath] = new CliEntityInfo(schema, schema, rootSupplemental);
			fileInfos.Add(rootFileInfo);

			// Root commands
			foreach (var cmd in schema.Commands)
			{
				var path = SyntheticPath(Build.DocumentationSourceDirectory.FullName, virtualRoot, [cmd.Name], isNamespace: false);
				var fileInfo = Build.ReadFileSystem.FileInfo.New(path);
				var supplemental = FindSupplemental(supplementalDirPath, [cmd.Name], isNamespace: false, matched);
				_syntheticFiles[path] = new CliEntityInfo(schema, cmd, supplemental);
				fileInfos.Add(fileInfo);
			}

			// Namespaces (recursive)
			CollectNamespaceFiles(Build.DocumentationSourceDirectory.FullName, virtualRoot, supplementalDirPath, schema.Namespaces, [], matched, fileInfos, schema);

			// Validate supplemental files
			if (supplementalDirPath is not null && Build.ReadFileSystem.Directory.Exists(supplementalDirPath))
				ValidateSupplementalFiles(supplementalDirPath, matched, cliRef.Context);
		}

		return fileInfos;
	}

	private void CollectNamespaceFiles(
		string docSourceDir,
		string virtualRoot,
		string? supplementalDirPath,
		IReadOnlyCollection<CliNamespaceSchema> namespaces,
		string[] nsPath,
		HashSet<string> matched,
		List<IFileInfo> fileInfos,
		ArghCliSchema schema)
	{
		foreach (var ns in namespaces)
		{
			var fullNsPath = nsPath.Append(ns.Segment).ToArray();

			var nsFilePath = SyntheticPath(docSourceDir, virtualRoot, fullNsPath, isNamespace: true);
			var nsFileInfo = Build.ReadFileSystem.FileInfo.New(nsFilePath);
			var nsSupplemental = FindSupplemental(supplementalDirPath, fullNsPath, isNamespace: true, matched);
			_syntheticFiles![nsFilePath] = new CliEntityInfo(schema, ns, nsSupplemental);
			fileInfos.Add(nsFileInfo);

			foreach (var cmd in ns.Commands)
			{
				var cmdSegments = fullNsPath.Append(cmd.Name).ToArray();
				var cmdPath = SyntheticPath(docSourceDir, virtualRoot, cmdSegments, isNamespace: false);
				var cmdFileInfo = Build.ReadFileSystem.FileInfo.New(cmdPath);
				var cmdSupplemental = FindSupplemental(supplementalDirPath, cmdSegments, isNamespace: false, matched);
				_syntheticFiles[cmdPath] = new CliEntityInfo(schema, cmd, cmdSupplemental);
				fileInfos.Add(cmdFileInfo);
			}

			CollectNamespaceFiles(docSourceDir, virtualRoot, supplementalDirPath, ns.Namespaces, fullNsPath, matched, fileInfos, schema);
		}
	}

	// Absolute synthetic path: docSourceDir/virtualRoot/segments.../index.md (namespace) or .../cmd-<name>.md (command)
	// Commands always use the cmd- prefix to avoid collisions with namespace index.md files.
	// GetFullPath normalizes any ".." segments so the key matches IFileInfo.FullName lookups.
	internal static string SyntheticPath(string docSourceDir, string virtualRoot, string[] segments, bool isNamespace)
	{
		if (segments.Length == 0)
			return Path.GetFullPath(Path.Join(docSourceDir, virtualRoot, "index.md"));

		if (isNamespace)
		{
			var joined = Path.Combine([.. segments]);
			return Path.GetFullPath(Path.Join(docSourceDir, virtualRoot, joined, "index.md"));
		}
		else
		{
			// Command pages: all parent segments as path, final name prefixed with cmd-
			var cmdName = $"cmd-{segments[^1]}";
			var parentSegments = segments.Length > 1 ? segments[..^1] : [];
			var parentPath = parentSegments.Length > 0 ? Path.Combine([.. parentSegments]) : string.Empty;
			return string.IsNullOrEmpty(parentPath)
				? Path.GetFullPath(Path.Join(docSourceDir, virtualRoot, cmdName + ".md"))
				: Path.GetFullPath(Path.Join(docSourceDir, virtualRoot, parentPath, cmdName + ".md"));
		}
	}

	private IFileInfo? FindSupplemental(string? supplementalDirPath, string[] segments, bool isNamespace, HashSet<string> matched)
	{
		if (supplementalDirPath is null)
			return null;

		var candidates = isNamespace
			? HierarchyCandidates(segments, isNamespace: true).Concat(FlatPrefixCandidates(segments, isNamespace: true))
			: HierarchyCandidates(segments, isNamespace: false).Concat(FlatPrefixCandidates(segments, isNamespace: false));

		foreach (var candidate in candidates)
		{
			var fullPath = Path.Join(supplementalDirPath, candidate);
			var fileInfo = Build.ReadFileSystem.FileInfo.New(fullPath);
			if (!fileInfo.Exists)
				continue;
			_ = matched.Add(fullPath);
			return fileInfo;
		}
		return null;
	}

	// hierarchy style for namespaces: changelog/index.md (natural folder layout) then changelog/ns-changelog.md
	// hierarchy style for commands: assembler/cmd-build.md
	private static IEnumerable<string> HierarchyCandidates(string[] segments, bool isNamespace)
	{
		if (segments.Length == 0)
		{
			yield return "ns-root.md";
			yield break;
		}

		if (isNamespace)
		{
			// index.md inside a subfolder named after the namespace path (e.g. changelog/index.md)
			var joinedPath = string.Join("/", segments);
			yield return $"{joinedPath}/index.md";
		}

		var prefix = isNamespace ? "ns-" : "cmd-";
		var lastName = segments[^1];
		var folder = segments.Length > 1 ? string.Join("/", segments[..^1]) + "/" : string.Empty;
		yield return $"{folder}{prefix}{lastName}.md";
	}

	// flat style: ns-assembler-content-source.md, cmd-assembler-build.md
	private static IEnumerable<string> FlatPrefixCandidates(string[] segments, bool isNamespace)
	{
		if (segments.Length == 0)
			yield break;

		var prefix = isNamespace ? "ns-" : "cmd-";
		var joined = string.Join("-", segments);
		yield return $"{prefix}{joined}.md";
	}

	private void ValidateSupplementalFiles(string supplementalDirPath, HashSet<string> matched, string context)
	{
		foreach (var file in Build.ReadFileSystem.Directory
			.EnumerateFiles(supplementalDirPath, "*.md", SearchOption.AllDirectories))
		{
			var name = Path.GetFileName(file);
			var relPath = Path.GetRelativePath(supplementalDirPath, file);

			if (name == "index.md")
			{
				// index.md at the supplemental root is not valid — must be inside a namespace subfolder
				if (Path.GetDirectoryName(relPath) is "" or null or ".")
					Build.Collector.EmitError(context, $"CLI supplemental docs folder must not contain a root-level index.md");
				else if (!matched.Contains(file))
					Build.Collector.EmitError(context, $"CLI supplemental 'index.md' at '{relPath}' does not match any CLI namespace (expected a subfolder named after the namespace path)");
				continue;
			}

			if (!name.StartsWith("ns-", StringComparison.OrdinalIgnoreCase) &&
				!name.StartsWith("cmd-", StringComparison.OrdinalIgnoreCase))
				continue;

			if (!matched.Contains(file))
				Build.Collector.EmitError(context, $"CLI supplemental file '{relPath}' does not match any CLI namespace or command");
		}
	}

	private static IReadOnlyCollection<CliReferenceRef> FindCliReferenceRefs(IReadOnlyCollection<ITableOfContentsItem> items)
	{
		var found = new List<CliReferenceRef>();
		Traverse(items, found);
		return found;

		static void Traverse(IReadOnlyCollection<ITableOfContentsItem> items, List<CliReferenceRef> found)
		{
			foreach (var item in items)
			{
				if (item is CliReferenceRef cliRef)
				{
					found.Add(cliRef);
					continue;
				}

				var children = item switch
				{
					FileRef f => f.Children,
					FolderRef f => f.Children,
					IsolatedTableOfContentsRef t => t.Children,
					_ => null
				};
				if (children is { Count: > 0 })
					Traverse(children, found);
			}
		}
	}
}
