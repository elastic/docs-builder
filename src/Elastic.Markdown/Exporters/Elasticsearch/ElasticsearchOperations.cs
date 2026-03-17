// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch;

/// <summary>
/// Shared Elasticsearch operations with retry logic and async task management.
/// Provides common infrastructure for ES API calls with exponential backoff on 429 errors
/// and polling for async operations (delete_by_query, reindex, update_by_query).
/// </summary>
public class ElasticsearchOperations(
	ITransport transport,
	ILogger logger,
	IDiagnosticsCollector? collector = null,
	int maxRetries = 5)
{
	private readonly ITransport _transport = transport;
	private readonly ILogger _logger = logger;
	private readonly IDiagnosticsCollector? _collector = collector;
	private readonly int _maxRetries = maxRetries;

	/// <summary>
	/// Executes an Elasticsearch API call with exponential backoff retry on transient errors (429, 5xx).
	/// </summary>
	public async Task<TResponse> WithRetryAsync<TResponse>(
		Func<Task<TResponse>> apiCall,
		string operationName,
		CancellationToken ct) where TResponse : TransportResponse
	{
		for (var attempt = 0; attempt <= _maxRetries; attempt++)
		{
			var response = await apiCall();

			if (response.ApiCallDetails.HasSuccessfulStatusCode)
				return response;

			var statusCode = response.ApiCallDetails.HttpStatusCode;
			var isRetryable = statusCode is 429 or >= 500;

			if (isRetryable && attempt < _maxRetries)
			{
				var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 1s, 2s, 4s, 8s, 16s
				_logger.LogWarning(
					"Retryable error ({StatusCode}) on {Operation}, retrying in {Delay}s (attempt {Attempt}/{MaxRetries})",
					statusCode, operationName, delay.TotalSeconds, attempt + 1, _maxRetries);
				await Task.Delay(delay, ct);
				continue;
			}

			// Not retryable or exhausted retries - return the response for caller to handle
			return response;
		}

		// Should never reach here, but satisfy compiler
		return await apiCall();
	}

	/// <summary>
	/// Polls an async Elasticsearch task until completion.
	/// </summary>
	public async Task PollTaskUntilCompleteAsync(
		string taskId,
		string operation,
		string sourceIndex,
		string? destIndex,
		CancellationToken ct)
	{
		bool completed;
		do
		{
			var taskResponse = await WithRetryAsync(
				() => _transport.GetAsync<DynamicResponse>($"/_tasks/{taskId}", ct),
				$"GET _tasks/{taskId}",
				ct);

			completed = taskResponse.Body.Get<bool>("completed");
			var total = taskResponse.Body.Get<int>("task.status.total");
			var updated = taskResponse.Body.Get<int>("task.status.updated");
			var created = taskResponse.Body.Get<int>("task.status.created");
			var deleted = taskResponse.Body.Get<int>("task.status.deleted");
			var batches = taskResponse.Body.Get<int>("task.status.batches");
			var runningTimeInNanos = taskResponse.Body.Get<long>("task.running_time_in_nanos");
			var time = TimeSpan.FromMicroseconds(runningTimeInNanos / 1000);

			if (destIndex is not null)
			{
				_logger.LogInformation(
					"{Operation}: {Time} '{SourceIndex}' => '{DestIndex}'. Documents {Total}: {Updated} updated, {Created} created, {Deleted} deleted, {Batches} batches",
					operation, time.ToString(@"hh\:mm\:ss"), sourceIndex, destIndex, total, updated, created, deleted, batches);
			}
			else
			{
				_logger.LogInformation(
					"{Operation} '{SourceIndex}': {Time} Documents {Total}: {Updated} updated, {Created} created, {Deleted} deleted, {Batches} batches",
					operation, sourceIndex, time.ToString(@"hh\:mm\:ss"), total, updated, created, deleted, batches);
			}

			if (!completed)
				await Task.Delay(TimeSpan.FromSeconds(5), ct);

		} while (!completed);
	}

	/// <summary>
	/// Executes an async POST operation (like delete_by_query, reindex) and returns the task ID.
	/// Use with wait_for_completion=false URLs.
	/// </summary>
	/// <returns>Task ID if successful, null if failed</returns>
	public async Task<string?> PostAsyncTaskAsync(
		string url,
		PostData request,
		string operationName,
		CancellationToken ct)
	{
		var response = await WithRetryAsync(
			() => _transport.PostAsync<DynamicResponse>(url, request, ct),
			operationName,
			ct);

		var taskId = response.Body.Get<string>("task");
		if (string.IsNullOrWhiteSpace(taskId))
		{
			_logger.LogError("Failed to start async task for {Operation}: {Response}", operationName, response);
			_collector?.EmitGlobalError($"Failed to start async task for {operationName}");
			return null;
		}

		_logger.LogDebug("{Operation} task id: {TaskId}", operationName, taskId);
		return taskId;
	}

	/// <summary>
	/// Executes a delete_by_query operation asynchronously (fire-and-forget).
	/// Returns the task ID without waiting for completion.
	/// </summary>
	public async Task<string?> DeleteByQueryFireAndForgetAsync(
		string index,
		PostData query,
		CancellationToken ct)
	{
		var url = $"/{index}/_delete_by_query?wait_for_completion=false";
		return await PostAsyncTaskAsync(url, query, $"POST {index}/_delete_by_query", ct);
	}

	/// <summary>
	/// Executes a delete_by_query operation and waits for completion.
	/// </summary>
	public async Task DeleteByQueryAsync(
		string index,
		PostData query,
		CancellationToken ct)
	{
		var taskId = await DeleteByQueryFireAndForgetAsync(index, query, ct);
		if (taskId is not null)
			await PollTaskUntilCompleteAsync(taskId, "_delete_by_query", index, null, ct);
	}

	/// <summary>
	/// Executes an update_by_query operation and waits for completion.
	/// </summary>
	public async Task UpdateByQueryAsync(
		string index,
		PostData query,
		string? pipeline,
		CancellationToken ct)
	{
		var pipelineParam = pipeline is not null ? $"&pipeline={pipeline}" : "";
		var url = $"/{index}/_update_by_query?wait_for_completion=false{pipelineParam}";
		var taskId = await PostAsyncTaskAsync(url, query, $"POST {index}/_update_by_query", ct);
		if (taskId is not null)
			await PollTaskUntilCompleteAsync(taskId, "_update_by_query", index, null, ct);
	}
}
