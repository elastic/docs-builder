// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Channels;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Serialization;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Catalog;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Indexing;

/// <summary>
/// Base class for Elasticsearch ingest channels with error tracking and fail-fast support.
/// </summary>
/// <typeparam name="TDocument">The document type being indexed.</typeparam>
/// <typeparam name="TChannelOptions">The channel options type.</typeparam>
/// <typeparam name="TChannel">The channel implementation type.</typeparam>
/// <summary>
/// Information about an index channel for display purposes.
/// </summary>
public record IndexChannelInfo(
	string Alias,
	string IndexName,
	bool IsReusing,
	string? ServerHash,
	string ChannelHash
);

public abstract class CrawlIndexerIngestChannel<TDocument, TChannelOptions, TChannel> : IDisposable
	where TDocument : class
	where TChannelOptions : CatalogIndexChannelOptionsBase<TDocument>
	where TChannel : CatalogIndexChannel<TDocument, TChannelOptions>
{
	public TChannel Channel { get; }
	public bool IsReusingIndex { get; private set; }
	public string? ServerHash { get; private set; }
	public string Alias { get; }

	private readonly ILogger _logger;
	private readonly IDiagnosticsCollector _diagnostics;
	private readonly IndexingErrorTracker _errorTracker;
	private readonly string _channelName;

	protected CrawlIndexerIngestChannel(
		ILoggerFactory logFactory,
		IDiagnosticsCollector diagnostics,
		IndexingErrorTracker errorTracker,
		ElasticsearchEndpoint endpoint,
		DistributedTransport transport,
		Func<TChannelOptions, TChannel> createChannel,
		Func<DistributedTransport, TChannelOptions> createOptions,
		string channelName,
		string alias
	)
	{
		_logger = logFactory.CreateLogger(GetType());
		_diagnostics = diagnostics;
		_errorTracker = errorTracker;
		_channelName = channelName;
		Alias = alias;

		var options = createOptions(transport);
		var exported = 0;

		options.BufferOptions = new BufferOptions
		{
			OutboundBufferMaxSize = endpoint.BufferSize,
			ExportMaxConcurrency = endpoint.IndexNumThreads,
			ExportMaxRetries = endpoint.MaxRetries
		};
		options.SerializerContext = SourceGenerationContext.Default;
		options.ExportBufferCallback = () =>
		{
			var count = Interlocked.Add(ref exported, endpoint.BufferSize);
			_logger.LogInformation("Exported {Count} documents to {Channel}", count, channelName);
		};
		options.ExportExceptionCallback = e =>
		{
			_logger.LogError(e, "Failed to export document to {Channel}", channelName);
			_diagnostics.EmitError(_channelName, "Failed to export document", e);
			_errorTracker.RecordException(e);
		};
		options.ServerRejectionCallback = items =>
		{
			foreach (var (_, responseItem) in items)
			{
				var msg = $"Server rejection: {responseItem.Status} {responseItem.Error?.Type} {responseItem.Error?.Reason}";
				_logger.LogWarning("{Message}", msg);
				_diagnostics.EmitError(_channelName, msg);
			}
		};

		Channel = createChannel(options);
		_logger.LogInformation("Created {Channel} channel", channelName);
	}

	/// <summary>
	/// Bootstraps the channel, detecting whether to reuse existing index based on mapping/settings hash.
	/// </summary>
	public async ValueTask<bool> StartAsync(Cancel ctx = default)
	{
		// Get current hash from server (if template exists)
		ServerHash = await Channel.GetIndexTemplateHashAsync(ctx) ?? string.Empty;

		// Bootstrap the channel (creates templates, index if needed)
		var bootstrapped = await Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);
		if (!bootstrapped)
		{
			_diagnostics.EmitError(_channelName, "Failed to bootstrap Elasticsearch");
			return false;
		}

		// Compare hashes to determine if we can reuse the existing index
		IsReusingIndex = !string.IsNullOrEmpty(ServerHash) && ServerHash == Channel.ChannelHash;

		if (IsReusingIndex)
			_logger.LogInformation("Reusing existing {Channel} index '{Index}' (hash match: {Hash})",
				_channelName, Channel.IndexName, ServerHash[..Math.Min(8, ServerHash.Length)]);
		else if (string.IsNullOrEmpty(ServerHash))
			_logger.LogInformation("Creating new {Channel} index '{Index}' (no existing template)",
				_channelName, Channel.IndexName);
		else
			_logger.LogInformation("Creating new {Channel} index '{Index}' (hash mismatch: server={ServerHash}, channel={ChannelHash})",
				_channelName, Channel.IndexName, ServerHash[..Math.Min(8, ServerHash.Length)], Channel.ChannelHash[..Math.Min(8, Channel.ChannelHash.Length)]);

		return true;
	}

	/// <summary>
	/// Gets information about this channel for display purposes.
	/// </summary>
	public IndexChannelInfo GetChannelInfo() =>
		new(Alias, Channel.IndexName ?? "", IsReusingIndex, ServerHash, Channel.ChannelHash);

	public async ValueTask<bool> TryWriteAsync(TDocument document, Cancel ctx = default)
	{
		if (Channel.TryWrite(document))
			return true;

		if (await Channel.WaitToWriteAsync(ctx))
			return Channel.TryWrite(document);

		return false;
	}

	public async ValueTask<bool> StopAsync(Cancel ctx = default)
	{
		_logger.LogInformation("Draining channel...");
		var drained = await Channel.WaitForDrainAsync(null, ctx);
		if (!drained)
			_diagnostics.EmitWarning(_channelName, "Failed to drain channel in time");

		_logger.LogInformation("Refreshing index...");
		var refreshed = await Channel.RefreshAsync(ctx);
		if (!refreshed)
			_diagnostics.EmitError(_channelName, "Failed to refresh index");

		_logger.LogInformation("Applying aliases...");
		var aliased = await Channel.ApplyAliasesAsync(ctx);
		if (!aliased)
			_diagnostics.EmitError(_channelName, "Failed to apply aliases");

		return drained && refreshed && aliased;
	}

	public async ValueTask DrainAsync(Cancel ctx = default)
	{
		_logger.LogInformation("Draining channel before disposal...");
		try
		{
			_ = await Channel.WaitForDrainAsync(TimeSpan.FromSeconds(30), ctx);
		}
		catch (OperationCanceledException)
		{
			_logger.LogWarning("Drain cancelled");
		}
	}

	public void Dispose()
	{
		try
		{
			Channel.Complete();
		}
		catch (Exception ex) when (ex.GetType().Name == "ChannelClosedException")
		{
			// Channel already closed, ignore
		}
		Channel.Dispose();
		GC.SuppressFinalize(this);
	}
}
