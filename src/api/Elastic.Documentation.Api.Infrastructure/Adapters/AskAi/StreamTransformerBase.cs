// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using Elastic.Documentation.Api.Core.AskAi;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

/// <summary>
/// Base class for stream transformers that handles common streaming logic
/// </summary>
public abstract class StreamTransformerBase(ILogger logger) : IStreamTransformer
{
	protected ILogger Logger { get; } = logger;

	public Task<Stream> TransformAsync(Stream rawStream, CancellationToken cancellationToken = default)
	{
		var pipe = new Pipe();
		var reader = new StreamReader(rawStream, Encoding.UTF8);

		// Start background task to transform and write events to pipe
		// Note: We intentionally don't await this task as we need to return the stream immediately
		// The pipe handles synchronization between the writer (background task) and reader (returned stream)
		var transformTask = Task.Run(async () =>
		{
			try
			{
				await ProcessStreamAsync(reader, pipe.Writer, cancellationToken);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Error transforming stream. Stream processing will be terminated.");
				await pipe.Writer.CompleteAsync(ex);
			}
			finally
			{
				reader.Dispose();
				await pipe.Writer.CompleteAsync();
			}
		}, cancellationToken);

		// Log any unhandled exceptions from the transform task
		_ = transformTask.ContinueWith(t =>
		{
			if (t.IsFaulted)
			{
				Logger.LogError(t.Exception, "Unhandled exception in stream transformation task");
			}
		}, TaskContinuationOptions.OnlyOnFaulted);

		// Return the read side of the pipe as a stream
		return Task.FromResult(pipe.Reader.AsStream());
	}

	/// <summary>
	/// Process the raw stream and write transformed events to the pipe writer
	/// </summary>
	protected abstract Task ProcessStreamAsync(StreamReader reader, PipeWriter writer, CancellationToken cancellationToken);

	/// <summary>
	/// Write a transformed event to the output stream
	/// </summary>
	protected async Task WriteEventAsync(AskAiEvent? transformedEvent, PipeWriter writer, CancellationToken cancellationToken)
	{
		if (transformedEvent == null)
			return;

		// Serialize as base AskAiEvent type to include the type discriminator
		var json = JsonSerializer.Serialize<AskAiEvent>(transformedEvent, AskAiEventJsonContext.Default.AskAiEvent);
		var sseData = $"data: {json}\n\n";
		var bytes = Encoding.UTF8.GetBytes(sseData);

		// Write to pipe and flush immediately for real-time streaming
		_ = await writer.WriteAsync(bytes, cancellationToken);
		_ = await writer.FlushAsync(cancellationToken);
	}
}
