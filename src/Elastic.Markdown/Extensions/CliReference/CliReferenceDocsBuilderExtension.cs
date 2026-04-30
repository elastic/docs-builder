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
	IFileInfo? SupplementalDoc,
	/// <summary>The clean synthetic file (no cmd- prefix) — used as the MarkdownFile source for correct URL generation.</summary>
	IFileInfo? CleanSyntheticFile = null,
	/// <summary>Full path segments used to build the page heading (e.g. ["assembler", "bloom-filter"]).</summary>
	string[]? FullPath = null
);

public class CliReferenceDocsBuilderExtension(BuildContext build) : IDocsBuilderExtension
{
	private BuildContext Build { get; } = build;

	private Dictionary<string, CliEntityInfo>? _syntheticFiles;
	private List<IFileInfo>? _syntheticFileInfos;
	// Maps physical supplemental file paths (cmd-*.md, index.md) → entity info with clean synthetic path
	private Dictionary<string, CliEntityInfo>? _supplementalFiles;
	// Cache of created MarkdownFile instances keyed by clean synthetic path — ensures the same instance
	// is returned from both CreateMarkdownFile (supplemental) and CreateDocumentationFile (synthetic),
	// so NavigationDocumentationFileLookup can find the file regardless of which key is used.
	private readonly Dictionary<string, MarkdownFile> _createdFiles = [];

	// Must be called before CreateMarkdownFile or CreateDocumentationFile can match anything.
	// ScanDocumentationFiles calls this; CreateMarkdownFile also triggers it because the main
	// directory scan runs before ScanDocumentationFiles, so index.md files are encountered first.
	private void EnsureSyntheticFilesBuilt()
	{
		if (_syntheticFiles is not null)
			return;
		_syntheticFiles = [];
		_supplementalFiles = [];
		_syntheticFileInfos = BuildSyntheticFiles();
	}

	public IDocumentationFileExporter? FileExporter => null;

	public DocumentationFile? CreateDocumentationFile(IFileInfo file, MarkdownParser markdownParser)
	{
		EnsureSyntheticFilesBuilt();
		if (!_syntheticFiles!.TryGetValue(file.FullName, out var info))
			return null;
		// Use the clean synthetic file as source if available (commands registered under clean path)
		var sourceFile = info.CleanSyntheticFile ?? file;
		// Return cached instance if CreateMarkdownFile already created it for this path
		if (_createdFiles.TryGetValue(sourceFile.FullName, out var cached))
			return cached;
		var result = CreateCliFileFromInfo(sourceFile, markdownParser, info);
		if (result != null)
			_createdFiles[sourceFile.FullName] = result;
		return result;
	}

	public MarkdownFile? CreateMarkdownFile(IFileInfo file, IDirectoryInfo sourceDirectory, MarkdownParser markdownParser)
	{
		// Physical CLI supplemental docs (index.md for namespaces, cmd-*.md for commands) need to be
		// intercepted before the factory creates a plain MarkdownFile for them.
		// EnsureSyntheticFilesBuilt() is called here because CreateMarkdownFile runs during the main
		// directory scan, before ScanDocumentationFiles populates the lookups.
		var name = file.Name;
		if (name != "index.md" && !name.StartsWith("cmd-", StringComparison.OrdinalIgnoreCase))
			return null;
		EnsureSyntheticFilesBuilt();
		var fullPath = Path.GetFullPath(file.FullName);

		// index.md: file path IS the synthetic path (namespace pages)
		if (_syntheticFiles!.TryGetValue(fullPath, out var info))
		{
			if (_createdFiles.TryGetValue(fullPath, out var cached))
				return cached;
			var result = CreateCliFileFromInfo(file, markdownParser, info);
			if (result != null)
				_createdFiles[fullPath] = result;
			return result;
		}

		// cmd-*.md: physical supplemental file — render as the associated CLI command page
		// using the clean synthetic path (no cmd- prefix) as the source file so RelativePath
		// and thus the output URL are both clean (e.g. apply.md → /cli/.../apply).
		if (_supplementalFiles!.TryGetValue(fullPath, out var suppInfo) && suppInfo.CleanSyntheticFile is not null)
		{
			var cleanPath = suppInfo.CleanSyntheticFile.FullName;
			if (_createdFiles.TryGetValue(cleanPath, out var cached))
				return cached;
			var result = CreateCliFileFromInfo(suppInfo.CleanSyntheticFile, markdownParser, suppInfo);
			if (result != null)
				_createdFiles[cleanPath] = result;
			return result;
		}

		return null;
	}

	private MarkdownFile? CreateCliFileFromInfo(IFileInfo sourceFile, MarkdownParser markdownParser, CliEntityInfo info) =>
		info.Entity switch
		{
			ArghCliSchema schema => new CliRootFile(sourceFile, Build.DocumentationSourceDirectory, markdownParser, Build, schema, info.SupplementalDoc),
			CliNamespaceSchema ns => new CliNamespaceFile(sourceFile, Build.DocumentationSourceDirectory, markdownParser, Build, ns, info.SupplementalDoc, info.FullPath ?? [ns.Segment], info.Schema.Name),
			CliCommandSchema cmd => new CliCommandFile(sourceFile, Build.DocumentationSourceDirectory, markdownParser, Build, cmd, info.SupplementalDoc, info.FullPath ?? [cmd.Name], info.Schema.Name),
			_ => null
		};

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
			var rootInfo = new CliEntityInfo(schema, schema, rootSupplemental, rootFileInfo);
			_syntheticFiles![rootSyntheticPath] = rootInfo;
			if (rootSupplemental != null)
				_supplementalFiles![rootSupplemental.FullName] = rootInfo;
			fileInfos.Add(rootFileInfo);

			// Root commands
			foreach (var cmd in schema.Commands)
			{
				var path = SyntheticPath(Build.DocumentationSourceDirectory.FullName, virtualRoot, [cmd.Name], isNamespace: false);
				var fileInfo = Build.ReadFileSystem.FileInfo.New(path);
				var supplemental = FindSupplemental(supplementalDirPath, [cmd.Name], isNamespace: false, matched);
				var cmdInfo = new CliEntityInfo(schema, cmd, supplemental, fileInfo, FullPath: [cmd.Name]);
				_syntheticFiles[path] = cmdInfo;
				if (supplemental != null)
					_supplementalFiles![supplemental.FullName] = cmdInfo;
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
			var nsInfo = new CliEntityInfo(schema, ns, nsSupplemental, nsFileInfo, FullPath: fullNsPath);
			_syntheticFiles![nsFilePath] = nsInfo;
			if (nsSupplemental != null)
				_supplementalFiles![nsSupplemental.FullName] = nsInfo;
			fileInfos.Add(nsFileInfo);

			foreach (var cmd in ns.Commands)
			{
				var cmdSegments = fullNsPath.Append(cmd.Name).ToArray();
				var cmdPath = SyntheticPath(docSourceDir, virtualRoot, cmdSegments, isNamespace: false);
				var cmdFileInfo = Build.ReadFileSystem.FileInfo.New(cmdPath);
				var cmdSupplemental = FindSupplemental(supplementalDirPath, cmdSegments, isNamespace: false, matched);
				var cmdInfo = new CliEntityInfo(schema, cmd, cmdSupplemental, cmdFileInfo, FullPath: cmdSegments);
				_syntheticFiles[cmdPath] = cmdInfo;
				if (cmdSupplemental != null)
					_supplementalFiles![cmdSupplemental.FullName] = cmdInfo;
				fileInfos.Add(cmdFileInfo);
			}

			CollectNamespaceFiles(docSourceDir, virtualRoot, supplementalDirPath, ns.Namespaces, fullNsPath, matched, fileInfos, schema);
		}
	}

	// Clean synthetic path (no cmd- prefix) — used for URL generation and Files registration.
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
			// Commands use clean name (no cmd- prefix) for URL e.g. /cli/assembler/deploy/apply.
			// Exception: commands named "index" must keep cmd- prefix to avoid collision with namespace index.md pages.
			var name = segments[^1].Equals("index", StringComparison.OrdinalIgnoreCase)
				? $"cmd-{segments[^1]}"
				: segments[^1];
			var parentSegments = segments.Length > 1 ? segments[..^1] : [];
			var parentPath = parentSegments.Length > 0 ? Path.Combine([.. parentSegments]) : string.Empty;
			return string.IsNullOrEmpty(parentPath)
				? Path.GetFullPath(Path.Join(docSourceDir, virtualRoot, name + ".md"))
				: Path.GetFullPath(Path.Join(docSourceDir, virtualRoot, parentPath, name + ".md"));
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
			yield return "index.md";
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
				if (!matched.Contains(file))
					Build.Collector.EmitError(context, $"CLI supplemental 'index.md' at '{relPath}' does not match any CLI namespace or the CLI root page");
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
