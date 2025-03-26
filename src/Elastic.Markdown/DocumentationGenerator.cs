// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Reflection;
using System.Text.Json;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.State;
using Elastic.Markdown.Links.CrossLinks;
using Elastic.Markdown.Slices;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown;

public interface IConversionCollector
{
	void Collect(MarkdownFile file, MarkdownDocument document, string html);
}

public interface IDocumentationFileOutputProvider
{
	IFileInfo? OutputFile(DocumentationSet documentationSet, IFileInfo defaultOutputFile, string relativePath);
}

public class DocumentationGenerator
{
	private readonly IDocumentationFileOutputProvider? _documentationFileOutputProvider;
	private readonly IConversionCollector? _conversionCollector;
	private readonly ILogger _logger;
	private readonly IFileSystem _writeFileSystem;
	private readonly IDocumentationFileExporter _documentationFileExporter;
	private HtmlWriter HtmlWriter { get; }

	public DocumentationSet DocumentationSet { get; }
	public BuildContext Context { get; }
	public ICrossLinkResolver Resolver { get; }

	public DocumentationGenerator(
		DocumentationSet docSet,
		ILoggerFactory logger,
		INavigationHtmlWriter? navigationHtmlWriter = null,
		IDocumentationFileOutputProvider? documentationFileOutputProvider = null,
		IDocumentationFileExporter? documentationExporter = null,
		IConversionCollector? conversionCollector = null
	)
	{
		_documentationFileOutputProvider = documentationFileOutputProvider;
		_conversionCollector = conversionCollector;
		_writeFileSystem = docSet.Build.WriteFileSystem;
		_logger = logger.CreateLogger(nameof(DocumentationGenerator));

		DocumentationSet = docSet;
		Context = docSet.Build;
		Resolver = docSet.LinkResolver;
		HtmlWriter = new HtmlWriter(DocumentationSet, _writeFileSystem, new DescriptionGenerator(), navigationHtmlWriter);
		_documentationFileExporter =
			documentationExporter
			?? docSet.Build.Configuration.EnabledExtensions.FirstOrDefault(e => e.FileExporter != null)?.FileExporter
			?? new DocumentationFileExporter(docSet.Build.ReadFileSystem, _writeFileSystem);

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

	public async Task GenerateAll(Cancel ctx)
	{
		var generationState = GetPreviousGenerationState();
		if (!Context.SkipMetadata && (Context.Force || generationState == null))
			DocumentationSet.ClearOutputDirectory();

		if (CompilationNotNeeded(generationState, out var offendingFiles, out var outputSeenChanges))
			return;

		_logger.LogInformation($"Fetching external links");
		_ = await Resolver.FetchLinks(ctx);

		await ResolveDirectoryTree(ctx);

		await ProcessDocumentationFiles(offendingFiles, outputSeenChanges, ctx);

		HintUnusedSubstitutionKeys();

		await ExtractEmbeddedStaticResources(ctx);

		if (Context.SkipMetadata)
			return;

		_logger.LogInformation($"Generating documentation compilation state");
		await GenerateDocumentationState(ctx);

		_logger.LogInformation($"Generating links.json");
		await GenerateLinkReference(ctx);
	}

	public async Task StopDiagnosticCollection(Cancel ctx)
	{
		_logger.LogInformation($"Completing diagnostics channel");
		Context.Collector.Channel.TryComplete();

		_logger.LogInformation($"Stopping diagnostics collector");
		await Context.Collector.StopAsync(ctx);

		_logger.LogInformation($"Completed diagnostics channel");
	}

	private async Task ProcessDocumentationFiles(HashSet<string> offendingFiles, DateTimeOffset outputSeenChanges, Cancel ctx)
	{
		var processedFileCount = 0;
		var exceptionCount = 0;
		var totalFileCount = DocumentationSet.Files.Count;
		_ = Context.Collector.StartAsync(ctx);
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
		// If we have less than 20 unused keys emit them separately
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
		var embeddedStaticFiles = Assembly.GetExecutingAssembly()
			.GetManifestResourceNames()
			.ToList();
		foreach (var a in embeddedStaticFiles)
		{
			await using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(a);
			if (resourceStream == null)
				continue;

			var path = a.Replace("Elastic.Markdown.", "").Replace("_static.", $"_static{Path.DirectorySeparatorChar}");

			var outputFile = OutputFile(path);
			if (outputFile is null)
				continue;
			await _documentationFileExporter.CopyEmbeddedResource(outputFile, resourceStream, ctx);
			_logger.LogDebug("Copied static embedded resource {Path}", path);
		}
	}

	private async Task ProcessFile(HashSet<string> offendingFiles, DocumentationFile file, DateTimeOffset outputSeenChanges, Cancel token)
	{
		if (!Context.Force)
		{
			if (offendingFiles.Contains(file.SourceFile.FullName))
				_logger.LogInformation("Re-evaluating {FileName}", file.SourceFile.FullName);
			else if (file.SourceFile.LastWriteTimeUtc <= outputSeenChanges)
				return;
		}

		_logger.LogTrace("--> {FileFullPath}", file.SourceFile.FullName);
		//TODO send file to OutputFile() so we can validate its scope is defined in navigation.yml
		var outputFile = OutputFile(file.RelativePath);
		if (outputFile is not null)
			await _documentationFileExporter.ProcessFile(Context, file, outputFile, HtmlWriter, _conversionCollector, token);
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

	private async Task GenerateLinkReference(Cancel ctx)
	{
		var file = DocumentationSet.LinkReferenceFile;
		var state = LinkReference.Create(DocumentationSet);

		var bytes = JsonSerializer.SerializeToUtf8Bytes(state, SourceGenerationContext.Default.LinkReference);
		await DocumentationSet.OutputDirectory.FileSystem.File.WriteAllBytesAsync(file.FullName, bytes, ctx);
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
