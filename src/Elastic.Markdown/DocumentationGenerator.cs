// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.RegularExpressions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Links;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Serialization;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Elastic.Documentation.State;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown;

/// Used primarily for testing, do not use in production paths since it might keep references alive to long
public interface IConversionCollector
{
	void Collect(MarkdownFile file, MarkdownDocument document, string html);
}

public interface IDocumentationFileOutputProvider
{
	IFileInfo? OutputFile(DocumentationSet documentationSet, IFileInfo defaultOutputFile, string relativePath);
}

public record GenerationResult
{
	public IReadOnlyDictionary<string, LinkRedirect> Redirects { get; set; } = new Dictionary<string, LinkRedirect>();
}

public partial class DocumentationGenerator
{
	private readonly IDocumentationFileOutputProvider? _documentationFileOutputProvider;
	private readonly IConversionCollector? _conversionCollector;
	private readonly ILogger _logger;
	private readonly IFileSystem _writeFileSystem;
	private readonly IDocumentationFileExporter _documentationFileExporter;
	private readonly IMarkdownExporter[] _markdownExporters;
	private readonly IDocumentInferrerService _documentInferrer;
	private HtmlWriter HtmlWriter { get; }

	public DocumentationSet DocumentationSet { get; }
	public BuildContext Context { get; }
	public IMarkdownStringRenderer MarkdownStringRenderer => HtmlWriter;

	public DocumentationGenerator(
		DocumentationSet docSet,
		ILoggerFactory logFactory,
		INavigationTraversable? positionalNavigation = null,
		INavigationHtmlWriter? navigationHtmlWriter = null,
		IDocumentationFileOutputProvider? documentationFileOutputProvider = null,
		IMarkdownExporter[]? markdownExporters = null,
		IConversionCollector? conversionCollector = null,
		ILegacyUrlMapper? legacyUrlMapper = null,
		IDocumentInferrerService? documentInferrer = null
	)
	{
		_markdownExporters = markdownExporters ?? [];
		_documentationFileOutputProvider = documentationFileOutputProvider;
		_conversionCollector = conversionCollector;
		_writeFileSystem = docSet.Context.WriteFileSystem;
		_logger = logFactory.CreateLogger(nameof(DocumentationGenerator));

		DocumentationSet = docSet;
		PositionalNavigation = positionalNavigation ?? docSet;
		Context = docSet.Context;

		// Use the provided inferrer or create a default one
		_documentInferrer = documentInferrer ?? new DocumentInferrerService(
			DocumentationSet.Context.ProductsConfiguration,
			DocumentationSet.Context.VersionsConfiguration,
			DocumentationSet.Context.LegacyUrlMappings,
			DocumentationSet.Configuration,
			DocumentationSet.Context.Git
		);

		HtmlWriter = new HtmlWriter(DocumentationSet, _writeFileSystem, new DescriptionGenerator(), positionalNavigation, navigationHtmlWriter, legacyUrlMapper, _documentInferrer);
		_documentationFileExporter =
			docSet.Context.AvailableExporters.Contains(Exporter.Html)
				? docSet.EnabledExtensions.FirstOrDefault(e => e.FileExporter != null)?.FileExporter
				  ?? new DocumentationFileExporter(docSet.Context.ReadFileSystem, _writeFileSystem)
				: new NoopDocumentationFileExporter();

		_logger.LogInformation("Created documentation set for: {DocumentationSetName}", DocumentationSet.Name);
		_logger.LogInformation("Source directory: {SourcePath} Exists: {SourcePathExists}", docSet.SourceDirectory, docSet.SourceDirectory.Exists);
		_logger.LogInformation("Output directory: {OutputPath} Exists: {OutputPathExists}", docSet.OutputDirectory, docSet.OutputDirectory.Exists);
	}

	private INavigationTraversable PositionalNavigation { get; }

	public GenerationState? GetPreviousGenerationState()
	{
		var stateFile = DocumentationSet.OutputStateFile;
		stateFile.Refresh();
		if (!stateFile.Exists)
			return null;
		var contents = stateFile.FileSystem.File.ReadAllText(stateFile.FullName);
		return JsonSerializer.Deserialize(contents, SourceGenerationContext.Default.GenerationState);
	}

	public async Task ResolveDirectoryTree(Cancel ctx)
	{
		_logger.LogInformation("Resolving tree");
		await DocumentationSet.ResolveDirectoryTree(ctx);
		_logger.LogInformation("Resolved tree");
	}

	public async Task<GenerationResult> GenerateAll(Cancel ctx)
	{
		var result = new GenerationResult();

		var generateState = Context.AvailableExporters.Contains(Exporter.DocumentationState);
		var generationState = !generateState ? null : GetPreviousGenerationState();

		// clear the output directory if force is true but never for assembler builds since these build multiple times to the output.
		if (Context is { BuildType: not BuildType.Assembler, Force: true }
			// clear the output directory if force is false but generation state is null, except for assembler builds.
			|| (Context is { BuildType: not BuildType.Assembler, Force: false } && generationState == null))
		{
			_logger.LogInformation($"Clearing output directory");
			DocumentationSet.ClearOutputDirectory();
		}

		var mode = GetCompilationMode(generationState, out var offendingFiles, out var outputSeenChanges);
		if (mode == CompilationMode.Skip)
			return result;

		await ResolveDirectoryTree(ctx);

		await ProcessDocumentationFiles(offendingFiles, outputSeenChanges, ctx);

		if (mode == CompilationMode.Full)
			HintUnusedSubstitutionKeys();

		await ExtractEmbeddedStaticResources(ctx);

		if (generateState)
		{
			_logger.LogInformation($"Generating documentation compilation state");
			await GenerateDocumentationState(ctx);
		}

		if (!Context.AvailableExporters.Overlaps([Exporter.LinkMetadata, Exporter.Redirects]))
			return result;

		_logger.LogInformation($"Generating links.json");
		var writeToDisk = Context.AvailableExporters.Contains(Exporter.LinkMetadata);
		var linkReference = await GenerateLinkReference(writeToDisk, ctx);

		return result with
		{
			Redirects = linkReference.Redirects ?? []
		};
	}

	private async Task ProcessDocumentationFiles(HashSet<string> offendingFiles, DateTimeOffset outputSeenChanges, Cancel ctx)
	{
		var processedFileCount = 0;
		var exceptionCount = 0;
		var totalFileCount = DocumentationSet.Files.Count;
		await Parallel.ForEachAsync(DocumentationSet.Files, ctx, async (file, token) =>
		{
			var processedFiles = Interlocked.Increment(ref processedFileCount);
			var (fp, doc) = file;
			try
			{
				await ProcessFile(offendingFiles, doc, outputSeenChanges, token);
			}
			catch (Exception e)
			{
				var currentCount = Interlocked.Increment(ref exceptionCount);
				// this is not the main error logging mechanism
				// if we hit this from too many files fail hard
				if (currentCount <= 25)
					Context.Collector.EmitError(fp.RelativePath, "Uncaught exception while processing file", e);
				else
					throw;
			}

			if (processedFiles % 100 == 0)
				_logger.LogInformation(" {Name} -> Processed {ProcessedFiles}/{TotalFileCount} files", Context.Git.RepositoryName, processedFiles, totalFileCount);
		});
		_logger.LogInformation(" {Name} -> Processed {ProcessedFileCount}/{TotalFileCount} files", Context.Git.RepositoryName, processedFileCount, totalFileCount);

	}

	private void HintUnusedSubstitutionKeys()
	{
		var definedKeys = new HashSet<string>(Context.Configuration.Substitutions.Keys.ToArray());
		var inUse = new HashSet<string>(Context.Collector.InUseSubstitutionKeys.Keys);
		var keysNotInUse = definedKeys.Except(inUse)
				// versions keys are injected
				.Where(key => !key.StartsWith("version."))
				// product keys are injected
				.Where(key => !key.StartsWith("product."))
				.Where(key => !key.StartsWith('.'))
				// reserving context namespace
				.Where(key => !key.StartsWith("context."))
				.ToArray();

		// If we have less than 20 unused keys, emit them separately,
		// Otherwise emit one hint with all of them for brevity
		if (keysNotInUse.Length >= 20)
		{
			var keys = string.Join(", ", keysNotInUse);
			Context.Collector.EmitHint(Context.ConfigurationPath.FullName, $"The following keys: '{keys}' are not used in any file");
		}
		else
		{
			foreach (var key in keysNotInUse)
				Context.Collector.EmitHint(Context.ConfigurationPath.FullName, $"Substitution key '{key}' is not used in any file");
		}
	}

	private async Task ExtractEmbeddedStaticResources(Cancel ctx)
	{
		// Skip copying static assets for codex builds - they are copied once to the root by CodexGenerator
		if (Context.BuildType == BuildType.Codex)
		{
			_logger.LogDebug("Skipping static asset extraction for codex documentation set (assets copied to root)");
			return;
		}

		_logger.LogInformation($"Copying static files to output directory");
		var assembly = typeof(EmbeddedOrPhysicalFileProvider).Assembly;
		var embeddedStaticFiles = assembly
			.GetManifestResourceNames()
			.ToList();
		foreach (var a in embeddedStaticFiles)
		{
			await using var resourceStream = assembly.GetManifestResourceStream(a);
			if (resourceStream == null)
				continue;

			var path = a.Replace("Elastic.Documentation.Site.", "").Replace("_static.", $"_static{Path.DirectorySeparatorChar}");

			var outputFile = OutputFile(path);
			if (outputFile is null)
				continue;
			await _documentationFileExporter.CopyEmbeddedResource(outputFile, resourceStream, ctx);
			_logger.LogDebug("Copied static embedded resource {Path}", path);
		}
	}

	[GeneratedRegex(@"^[a-z0-9\s\-_\.\/\\+]*[a-z0-9_\-+]\.([a-z]+)$")]
	private static partial Regex FilePathRegex();

	[GeneratedRegex(@"^[a-z0-9_][a-z0-9_\-\s\.+]*?\.([a-z]+)$")]
	private static partial Regex FileNameRegex();

	public static bool IsValidFileName(string strToCheck) =>
		strToCheck switch
		{
			//prior art
			_ when strToCheck.StartsWith("release-notes/elastic-agent/_snippets/") => true,
			_ when strToCheck.StartsWith("reference/query-languages/esql/_snippets/") => true,
			_ when strToCheck.EndsWith(".svg") => true,
			_ when strToCheck.EndsWith(".gif") => true,
			_ when strToCheck.EndsWith(".png") => true,
			_ when strToCheck.EndsWith(".png") => true,
			"reference/security/prebuilt-rules/audit_policies/windows/README.md" => true,
			"audit_policies/windows/README.md" => true,
			"extend/integrations/developer-workflow-fleet-UI.md" => true,
			"extend/developer-workflow-fleet-UI.md" => true,
			"reference/elasticsearch/clients/ruby/Helpers.md" => true,
			"reference/Helpers.md" => true,
			"explore-analyze/ai-features/llm-guides/connect-to-vLLM.md" => true,
			_ => FilePathRegex().IsMatch(strToCheck) && FileNameRegex().IsMatch(Path.GetFileName(strToCheck))
		};

	private async Task ProcessFile(HashSet<string> offendingFiles, DocumentationFile file, DateTimeOffset outputSeenChanges, Cancel ctx)
	{
		if (!Context.Force)
		{
			if (offendingFiles.Contains(file.SourceFile.FullName))
				_logger.LogInformation("Re-evaluating {FileName}", file.SourceFile.FullName);
			else if (file.SourceFile.LastWriteTimeUtc <= outputSeenChanges)
				return;
		}

		_logger.LogTrace("--> {FileFullPath}", file.SourceFile.FullName);
		var outputFile = OutputFile(file.RelativePath);

		if (outputFile is not null)
		{
			var relative = Path.GetRelativePath(Context.OutputDirectory.FullName, outputFile.FullName);
			if (!IsValidFileName(relative))
			{
				Context.Collector.EmitError(file.SourceFile.FullName, $"File name {relative} is not valid needs to be lowercase and contain only alphanumeric characters, spaces, dashes, dots, underscores, and plus signs");
				return;
			}

			var context = new ProcessingFileContext
			{
				BuildContext = Context,
				OutputFile = outputFile,
				ConversionCollector = _conversionCollector,
				File = file,
				HtmlWriter = HtmlWriter
			};
			await _documentationFileExporter.ProcessFile(context, ctx);
			if (file is MarkdownFile markdown)
			{
				foreach (var exporter in _markdownExporters)
				{
					var document = context.MarkdownDocument ??= await markdown.ParseFullAsync(DocumentationSet.TryFindDocumentByRelativePath, ctx);
					var navigationItem = PositionalNavigation.GetNavigationFor(markdown);
					_ = await exporter.ExportAsync(new MarkdownExportFileContext
					{
						BuildContext = Context,
						Resolvers = DocumentationSet.MarkdownParser.Resolvers,
						Document = document,
						SourceFile = markdown,
						DefaultOutputFile = outputFile,
						DocumentationSet = DocumentationSet,
						PositionaNavigation = PositionalNavigation,
						NavigationItem = navigationItem,
						InferenceService = _documentInferrer
					}, ctx);
				}
			}
		}
	}

	private IFileInfo? OutputFile(string relativePath)
	{
		var outputFile = _writeFileSystem.FileInfo.New(Path.Combine(DocumentationSet.OutputDirectory.FullName, relativePath));
		if (relativePath.StartsWith("_static"))
			return outputFile;

		return _documentationFileOutputProvider is not null
			? _documentationFileOutputProvider.OutputFile(DocumentationSet, outputFile, relativePath)
			: outputFile;
	}

	private enum CompilationMode { Full, Incremental, Skip }

	private CompilationMode GetCompilationMode(GenerationState? generationState, out HashSet<string> offendingFiles,
		out DateTimeOffset outputSeenChanges)
	{
		offendingFiles = [.. generationState?.InvalidFiles ?? []];
		outputSeenChanges = generationState?.LastSeenChanges ?? DateTimeOffset.MinValue;
		if (generationState == null)
			return CompilationMode.Full;

		if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
			return CompilationMode.Full;

		if (Context.Force)
		{
			_logger.LogInformation("Full compilation: --force was specified");
			return CompilationMode.Full;
		}

		if (Context.Git != generationState.Git)
		{
			_logger.LogInformation("Full compilation: current git context: {CurrentGitContext} differs from previous git context: {PreviousGitContext}",
				Context.Git, generationState.Git);
			return CompilationMode.Full;
		}

		if (offendingFiles.Count > 0)
		{
			_logger.LogInformation("Incremental compilation. since: {LastWrite}", DocumentationSet.LastWrite);
			_logger.LogInformation("Incremental compilation. {FileCount} files with errors/warnings", offendingFiles.Count);
			return CompilationMode.Incremental;
		}
		else if (DocumentationSet.LastWrite > outputSeenChanges)
		{
			_logger.LogInformation("Incremental compilation. since: {LastSeenChanges}", generationState.LastSeenChanges);
			return CompilationMode.Incremental;
		}
		else if (DocumentationSet.LastWrite <= outputSeenChanges)
		{
			_logger.LogInformation(
				"No compilation: no changes since last observed: {LastSeenChanges}. " +
				"Pass --force to force a full regeneration", generationState.LastSeenChanges
			);
			return CompilationMode.Skip;
		}

		return CompilationMode.Full;
	}

	private async Task<RepositoryLinks> GenerateLinkReference(bool writeToDisk, Cancel ctx)
	{
		var file = DocumentationSet.LinkReferenceFile;
		var state = DocumentationSet.CreateLinkReference();
		if (writeToDisk)
		{
			if (!file.Directory!.Exists)
				file.Directory.Create();
			var bytes = JsonSerializer.SerializeToUtf8Bytes(state, SourceGenerationContext.Default.RepositoryLinks);
			await DocumentationSet.OutputDirectory.FileSystem.File.WriteAllBytesAsync(file.FullName, bytes, ctx);
		}
		return state;
	}

	private async Task GenerateDocumentationState(Cancel ctx)
	{
		var stateFile = DocumentationSet.OutputStateFile;
		_logger.LogInformation("Writing documentation state {LastWrite} to {StateFileName}", DocumentationSet.LastWrite, stateFile.FullName);
		var badFiles = Context.Collector.OffendingFiles.ToArray();
		var state = new GenerationState
		{
			LastSeenChanges = DocumentationSet.LastWrite,
			InvalidFiles = badFiles,
			Git = Context.Git,
			Exporter = _documentationFileExporter.Name
		};
		var bytes = JsonSerializer.SerializeToUtf8Bytes(state, SourceGenerationContext.Default.GenerationState);
		await DocumentationSet.OutputDirectory.FileSystem.File.WriteAllBytesAsync(stateFile.FullName, bytes, ctx);
	}

	public async Task<string> RenderLlmMarkdown(MarkdownFile markdown, Cancel ctx)
	{
		await DocumentationSet.ResolveDirectoryTree(ctx);
		var document = await markdown.ParseFullAsync(DocumentationSet.TryFindDocumentByRelativePath, ctx);
		return LlmMarkdownExporter.ConvertToLlmMarkdown(document, DocumentationSet.Context);
	}

	public async Task<RenderResult> RenderLayout(MarkdownFile markdown, Cancel ctx)
	{
		await DocumentationSet.ResolveDirectoryTree(ctx);
		return await HtmlWriter.RenderLayout(markdown, ctx);
	}

}
