// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Markdown.IO;
using Elastic.Markdown.Slices;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(OutputState))]
internal partial class SourceGenerationContext : JsonSerializerContext;

public class OutputState
{
	public DateTimeOffset LastSeenChanges { get; set; }
	public string[] Conflict { get; set; } = [];
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
		BuildContext context,
		ILoggerFactory logger
	)
	{
		_readFileSystem = context.ReadFileSystem;
		_writeFileSystem = context.WriteFileSystem;
		_logger = logger.CreateLogger(nameof(DocumentationGenerator));

		DocumentationSet = docSet;
		Context = context;
		HtmlWriter = new HtmlWriter(DocumentationSet, _writeFileSystem);

		_logger.LogInformation($"Created documentation set for: {DocumentationSet.Name}");
		_logger.LogInformation($"Source directory: {docSet.SourcePath} Exists: {docSet.SourcePath.Exists}");
		_logger.LogInformation($"Output directory: {docSet.OutputPath} Exists: {docSet.OutputPath.Exists}");
	}

	public static DocumentationGenerator Create(
		string? path,
		string? output,
		BuildContext context,
		ILoggerFactory logger
	)
	{
		var sourcePath = path != null ? context.ReadFileSystem.DirectoryInfo.New(path) : null;
		var outputPath = output != null ? context.WriteFileSystem.DirectoryInfo.New(output) : null;
		var docSet = new DocumentationSet(sourcePath, outputPath, context);
		return new DocumentationGenerator(docSet, context, logger);
	}

	public OutputState? OutputState
	{
		get
		{
			var stateFile = DocumentationSet.OutputStateFile;
			stateFile.Refresh();
			if (!stateFile.Exists) return null;
			var contents = stateFile.FileSystem.File.ReadAllText(stateFile.FullName);
			return JsonSerializer.Deserialize(contents, SourceGenerationContext.Default.OutputState);


		}
	}


	public async Task ResolveDirectoryTree(Cancel ctx) =>
		await DocumentationSet.Tree.Resolve(ctx);

	public async Task GenerateAll(Cancel ctx)
	{
		if (Context.Force || OutputState == null)
			DocumentationSet.ClearOutputDirectory();

		_logger.LogInformation($"Last write source: {DocumentationSet.LastWrite}, output observed: {OutputState?.LastSeenChanges}");

		var offendingFiles = new HashSet<string>(OutputState?.Conflict ?? []);
		var outputSeenChanges = OutputState?.LastSeenChanges ?? DateTimeOffset.MinValue;
		if (offendingFiles.Count > 0)
		{
			_logger.LogInformation($"Reapplying changes since {DocumentationSet.LastWrite}");
			_logger.LogInformation($"Reapplying for {offendingFiles.Count} files with errors/warnings");
		}
		else if (DocumentationSet.LastWrite > outputSeenChanges && OutputState != null)
			_logger.LogInformation($"Using incremental build picking up changes since: {OutputState.LastSeenChanges}");
		else if (DocumentationSet.LastWrite <= outputSeenChanges && OutputState != null)
		{
			_logger.LogInformation($"No changes in source since last observed write {OutputState.LastSeenChanges} "
			                       + "Pass --force to force a full regeneration");
			return;
		}

		_logger.LogInformation("Resolving tree");
		await ResolveDirectoryTree(ctx);
		_logger.LogInformation("Resolved tree");


		var handledItems = 0;

		var collectTask =  Task.Run(async () => await Context.Collector.StartAsync(ctx), ctx);

		await Parallel.ForEachAsync(DocumentationSet.Files, ctx, async (file, token) =>
		{
			if (offendingFiles.Contains(file.SourceFile.FullName))
				_logger.LogInformation($"Re-evaluating {file.SourceFile.FullName}");
			else if (file.SourceFile.LastWriteTimeUtc <= outputSeenChanges)
				return;

			var item = Interlocked.Increment(ref handledItems);
			var outputFile = OutputFile(file.RelativePath);
			if (file is MarkdownFile markdown)
			{
				await markdown.ParseFullAsync(token);
				await HtmlWriter.WriteAsync(outputFile, markdown, token);
			}
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

		await collectTask;
		await Context.Collector.Channel.Reader.Completion;
		await Context.Collector.StopAsync(ctx);


		IFileInfo OutputFile(string relativePath)
		{
			var outputFile = _writeFileSystem.FileInfo.New(Path.Combine(DocumentationSet.OutputPath.FullName, relativePath));
			return outputFile;
		}

	}

	private async Task GenerateDocumentationState(Cancel ctx)
	{
		var stateFile = DocumentationSet.OutputStateFile;
		_logger.LogInformation($"Writing documentation state {DocumentationSet.LastWrite} to {stateFile.FullName}");
		var badFiles = Context.Collector.OffendingFiles.ToArray();
		var state = new OutputState
		{
			LastSeenChanges = DocumentationSet.LastWrite,
			Conflict = badFiles

		};
		var bytes = JsonSerializer.SerializeToUtf8Bytes(state, SourceGenerationContext.Default.OutputState);
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
