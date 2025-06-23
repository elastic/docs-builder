// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Legacy;
using Elastic.Documentation.Links;
using Elastic.Documentation.Serialization;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Elastic.Documentation.State;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.Links.CrossLinks;
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

public class DocumentationGenerator
{
	private readonly IDocumentationFileOutputProvider? _documentationFileOutputProvider;
	private readonly IConversionCollector? _conversionCollector;
	private readonly ILogger _logger;
	private readonly IFileSystem _writeFileSystem;
	private readonly IDocumentationFileExporter _documentationFileExporter;
	private readonly IMarkdownExporter[] _markdownExporters;
	private HtmlWriter HtmlWriter { get; }

	public DocumentationSet DocumentationSet { get; }
	public BuildContext Context { get; }
	public ICrossLinkResolver Resolver { get; }
	public IMarkdownStringRenderer MarkdownStringRenderer => HtmlWriter;

	public DocumentationGenerator(
		DocumentationSet docSet,
		ILoggerFactory logger,
		INavigationHtmlWriter? navigationHtmlWriter = null,
		IDocumentationFileOutputProvider? documentationFileOutputProvider = null,
		IMarkdownExporter[]? markdownExporters = null,
		IDocumentationFileExporter? documentationExporter = null,
		IConversionCollector? conversionCollector = null,
		ILegacyUrlMapper? legacyUrlMapper = null,
		IPositionalNavigation? positionalNavigation = null
	)
	{
		_markdownExporters = markdownExporters ?? [];
		_documentationFileOutputProvider = documentationFileOutputProvider;
		_conversionCollector = conversionCollector;
		_writeFileSystem = docSet.Context.WriteFileSystem;
		_logger = logger.CreateLogger(nameof(DocumentationGenerator));

		DocumentationSet = docSet;
		Context = docSet.Context;
		Resolver = docSet.LinkResolver;
		HtmlWriter = new HtmlWriter(DocumentationSet, _writeFileSystem, new DescriptionGenerator(), navigationHtmlWriter, legacyUrlMapper,
			positionalNavigation);
		_documentationFileExporter =
			documentationExporter
			?? docSet.EnabledExtensions.FirstOrDefault(e => e.FileExporter != null)?.FileExporter
			?? new DocumentationFileExporter(docSet.Context.ReadFileSystem, _writeFileSystem);

		_logger.LogInformation("Created documentation set for: {DocumentationSetName}", DocumentationSet.Name);
		_logger.LogInformation("Source directory: {SourcePath} Exists: {SourcePathExists}", docSet.SourceDirectory, docSet.SourceDirectory.Exists);
		_logger.LogInformation("Output directory: {OutputPath} Exists: {OutputPathExists}", docSet.OutputDirectory, docSet.OutputDirectory.Exists);
	}

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
		await DocumentationSet.Tree.Resolve(ctx);
		_logger.LogInformation("Resolved tree");
	}

	public async Task<GenerationResult> GenerateAll(Cancel ctx)
	{
		var result = new GenerationResult();

		var generationState = Context.SkipDocumentationState ? null : GetPreviousGenerationState();

		// clear the output directory if force is true but never for assembler builds since these build multiple times to the output.
		if (Context is { AssemblerBuild: false, Force: true }
			// clear the output directory if force is false but generation state is null, except for assembler builds.
			|| (Context is { AssemblerBuild: false, Force: false } && generationState == null))
		{
			_logger.LogInformation($"Clearing output directory");
			DocumentationSet.ClearOutputDirectory();
		}

		if (CompilationNotNeeded(generationState, out var offendingFiles, out var outputSeenChanges))
			return result;

		_logger.LogInformation($"Fetching external links");
		_ = await Resolver.FetchLinks(ctx);

		await ResolveDirectoryTree(ctx);

		await ProcessDocumentationFiles(offendingFiles, outputSeenChanges, ctx);

		HintUnusedSubstitutionKeys();

		await ExtractEmbeddedStaticResources(ctx);

		if (!Context.SkipDocumentationState)
		{
			_logger.LogInformation($"Generating documentation compilation state");
			await GenerateDocumentationState(ctx);
		}

		_logger.LogInformation($"Generating links.json");
		var linkReference = await GenerateLinkReference(ctx);

		// ReSharper disable once WithExpressionModifiesAllMembers
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
			try
			{
				await ProcessFile(offendingFiles, file, outputSeenChanges, token);
			}
			catch (Exception e)
			{
				var currentCount = Interlocked.Increment(ref exceptionCount);
				// this is not the main error logging mechanism
				// if we hit this from too many files fail hard
				if (currentCount <= 25)
					Context.Collector.EmitError(file.RelativePath, "Uncaught exception while processing file", e);
				else
					throw;
			}

			if (processedFiles % 100 == 0)
				_logger.LogInformation("-> Processed {ProcessedFiles}/{TotalFileCount} files", processedFiles, totalFileCount);
		});
		_logger.LogInformation("-> Processed {ProcessedFileCount}/{TotalFileCount} files", processedFileCount, totalFileCount);
	}

	private void HintUnusedSubstitutionKeys()
	{
		var definedKeys = new HashSet<string>(Context.Configuration.Substitutions.Keys.ToArray());
		var inUse = new HashSet<string>(Context.Collector.InUseSubstitutionKeys.Keys);
		var keysNotInUse = definedKeys.Except(inUse).ToArray();
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
					var document = context.MarkdownDocument ??= await markdown.ParseFullAsync(ctx);
					_ = await exporter.ExportAsync(new MarkdownExportContext
					{
						BuildContext = Context,
						Resolvers = DocumentationSet.MarkdownParser.Resolvers,
						Document = document,
						File = markdown
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

	private bool CompilationNotNeeded(GenerationState? generationState, out HashSet<string> offendingFiles,
		out DateTimeOffset outputSeenChanges)
	{
		offendingFiles = [.. generationState?.InvalidFiles ?? []];
		outputSeenChanges = generationState?.LastSeenChanges ?? DateTimeOffset.MinValue;
		if (generationState == null)
			return false;
		if (Context.Force)
		{
			_logger.LogInformation("Full compilation: --force was specified");
			return false;
		}

		if (Context.Git != generationState.Git)
		{
			_logger.LogInformation("Full compilation: current git context: {CurrentGitContext} differs from previous git context: {PreviousGitContext}",
				Context.Git, generationState.Git);
			return false;
		}

		if (offendingFiles.Count > 0)
		{
			_logger.LogInformation("Incremental compilation. since: {LastWrite}", DocumentationSet.LastWrite);
			_logger.LogInformation("Incremental compilation. {FileCount} files with errors/warnings", offendingFiles.Count);
		}
		else if (DocumentationSet.LastWrite > outputSeenChanges)
			_logger.LogInformation("Incremental compilation. since: {LastSeenChanges}", generationState.LastSeenChanges);
		else if (DocumentationSet.LastWrite <= outputSeenChanges)
		{
			_logger.LogInformation(
				"No compilation: no changes since last observed: {LastSeenChanges}. " +
				"Pass --force to force a full regeneration", generationState.LastSeenChanges
			);
			return true;
		}

		return false;
	}

	private async Task<RepositoryLinks> GenerateLinkReference(Cancel ctx)
	{
		var file = DocumentationSet.LinkReferenceFile;
		var state = DocumentationSet.CreateLinkReference();
		var bytes = JsonSerializer.SerializeToUtf8Bytes(state, SourceGenerationContext.Default.RepositoryLinks);
		await DocumentationSet.OutputDirectory.FileSystem.File.WriteAllBytesAsync(file.FullName, bytes, ctx);
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

	public async Task<string?> RenderLayout(MarkdownFile markdown, Cancel ctx)
	{
		await DocumentationSet.Tree.Resolve(ctx);
		return await HtmlWriter.RenderLayout(markdown, ctx);
	}
}
