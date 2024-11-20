// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Markdown.IO;
using Elastic.Markdown.Slices;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(GenerationState))]
[JsonSerializable(typeof(LinkReference))]
[JsonSerializable(typeof(GitConfiguration))]
internal partial class SourceGenerationContext : JsonSerializerContext;

public record GenerationState
{
	[JsonPropertyName("last_seen_changes")]
	public required DateTimeOffset LastSeenChanges { get; init; }
	[JsonPropertyName("invalid_files")]
	public required string[] InvalidFiles { get; init; } = [];

	[JsonPropertyName("git")]
	public required GitConfiguration Git { get; init; }
}

public class DocumentationGenerator
{
	private readonly IFileSystem _readFileSystem;
	private readonly ILogger _logger;
	private readonly IFileSystem _writeFileSystem;
	private HtmlWriter HtmlWriter { get; }

	public DocumentationSet DocumentationSet { get; }
	public BuildContext Context { get; }

	public DocumentationGenerator(
		DocumentationSet docSet,
		ILoggerFactory logger
	)
	{
		_readFileSystem = docSet.Context.ReadFileSystem;
		_writeFileSystem = docSet.Context.WriteFileSystem;
		_logger = logger.CreateLogger(nameof(DocumentationGenerator));

		DocumentationSet = docSet;
		Context = docSet.Context;
		HtmlWriter = new HtmlWriter(DocumentationSet, _writeFileSystem);

		_logger.LogInformation($"Created documentation set for: {DocumentationSet.Name}");
		_logger.LogInformation($"Source directory: {docSet.SourcePath} Exists: {docSet.SourcePath.Exists}");
		_logger.LogInformation($"Output directory: {docSet.OutputPath} Exists: {docSet.OutputPath.Exists}");
	}

	public GenerationState? GetPreviousGenerationState()
	{
		var stateFile = DocumentationSet.OutputStateFile;
		stateFile.Refresh();
		if (!stateFile.Exists) return null;
		var contents = stateFile.FileSystem.File.ReadAllText(stateFile.FullName);
		return JsonSerializer.Deserialize(contents, SourceGenerationContext.Default.GenerationState);
	}


	public async Task ResolveDirectoryTree(Cancel ctx) =>
		await DocumentationSet.Tree.Resolve(ctx);

	public async Task GenerateAll(Cancel ctx)
	{
		var generationState = GetPreviousGenerationState();
		if (Context.Force || generationState == null)
			DocumentationSet.ClearOutputDirectory();

		if (CompilationNotNeeded(generationState, out var offendingFiles, out var outputSeenChanges))
			return;

		_logger.LogInformation("Resolving tree");
		await ResolveDirectoryTree(ctx);
		_logger.LogInformation("Resolved tree");


		var handledItems = 0;

		_ = Context.Collector.StartAsync(ctx);

		await Parallel.ForEachAsync(DocumentationSet.Files, ctx, async (file, token) =>
		{
			if (offendingFiles.Contains(file.SourceFile.FullName))
				_logger.LogInformation($"Re-evaluating {file.SourceFile.FullName}");
			else if (file.SourceFile.LastWriteTimeUtc <= outputSeenChanges)
				return;

			var item = Interlocked.Increment(ref handledItems);
			var outputFile = OutputFile(file.RelativePath);
			if (file is MarkdownFile markdown)
				await HtmlWriter.WriteAsync(outputFile, markdown, token);
			else
			{
				if (outputFile.Directory is { Exists: false })
					outputFile.Directory.Create();
				await CopyFileFsAware(file, outputFile, ctx);
			}
			if (item % 1_000 == 0)
				_logger.LogInformation($"Handled {handledItems} files");
		});
		Context.Collector.Channel.TryComplete();

		await GenerateDocumentationState(ctx);
		await GenerateLinkReference(ctx);

		await Context.Collector.StopAsync(ctx);

		IFileInfo OutputFile(string relativePath)
		{
			var outputFile = _writeFileSystem.FileInfo.New(Path.Combine(DocumentationSet.OutputPath.FullName, relativePath));
			return outputFile;
		}

	}

	private bool CompilationNotNeeded(GenerationState? generationState, out HashSet<string> offendingFiles,
		out DateTimeOffset outputSeenChanges)
	{
		offendingFiles = new HashSet<string>(generationState?.InvalidFiles ?? []);
		outputSeenChanges = generationState?.LastSeenChanges ?? DateTimeOffset.MinValue;
		if (generationState == null)
			return false;

		if (Context.Git != generationState.Git)
		{
			_logger.LogInformation($"Full compilation: current git context: {Context.Git} differs from previous git context: {generationState.Git}");
			return false;
		}

		if (offendingFiles.Count > 0)
		{
			_logger.LogInformation($"Incremental compilation. since: {DocumentationSet.LastWrite}");
			_logger.LogInformation($"Incremental compilation. {offendingFiles.Count} files with errors/warnings");
		}
		else if (DocumentationSet.LastWrite > outputSeenChanges)
			_logger.LogInformation($"Incremental compilation. since: {generationState.LastSeenChanges}");
		else if (DocumentationSet.LastWrite <= outputSeenChanges)
		{
			_logger.LogInformation($"No compilation: no changes since last observed: {generationState.LastSeenChanges}");
			_logger.LogInformation($"No compilation: no changes since last observed: {generationState.LastSeenChanges} "
			                       + "Pass --force to force a full regeneration");
			return true;
		}

		return false;
	}

	private async Task GenerateLinkReference(Cancel ctx)
	{
		var file = DocumentationSet.LinkReferenceFile;
		var state = LinkReference.Create(DocumentationSet);
		var bytes = JsonSerializer.SerializeToUtf8Bytes(state, SourceGenerationContext.Default.LinkReference);
		await DocumentationSet.OutputPath.FileSystem.File.WriteAllBytesAsync(file.FullName, bytes, ctx);
	}

	private async Task GenerateDocumentationState(Cancel ctx)
	{
		var stateFile = DocumentationSet.OutputStateFile;
		_logger.LogInformation($"Writing documentation state {DocumentationSet.LastWrite} to {stateFile.FullName}");
		var badFiles = Context.Collector.OffendingFiles.ToArray();
		var state = new GenerationState
		{
			LastSeenChanges = DocumentationSet.LastWrite,
			InvalidFiles = badFiles,
			Git = Context.Git
		};
		var bytes = JsonSerializer.SerializeToUtf8Bytes(state, SourceGenerationContext.Default.GenerationState);
		await DocumentationSet.OutputPath.FileSystem.File.WriteAllBytesAsync(stateFile.FullName, bytes, ctx);
	}

	private async Task CopyFileFsAware(DocumentationFile file, IFileInfo outputFile, Cancel ctx)
	{
		// fast path, normal case.
		if (_readFileSystem == _writeFileSystem)
			_readFileSystem.File.Copy(file.SourceFile.FullName, outputFile.FullName, true);
		//slower when we are mocking the write filesystem
		else
		{
			var bytes = await file.SourceFile.FileSystem.File.ReadAllBytesAsync(file.SourceFile.FullName, ctx);
			await outputFile.FileSystem.File.WriteAllBytesAsync(outputFile.FullName, bytes, ctx);
		}
	}

	public async Task<string?> RenderLayout(MarkdownFile markdown, Cancel ctx)
	{
		await DocumentationSet.Tree.Resolve(ctx);
		return await HtmlWriter.RenderLayout(markdown, ctx);
	}
}
