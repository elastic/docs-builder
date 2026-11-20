// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Elastic.Documentation.Mcp.Remote.Responses;
using Elastic.Documentation.Mcp.Remote.Telemetry;
using Elastic.Documentation.Search;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace Elastic.Documentation.Mcp.Remote.Tools;

/// <summary>What a tool handler produced: a JSON reply, or a reason it declined the call.</summary>
public readonly union ToolReply(ToolSucceeded, ToolRejected);

public sealed record ToolSucceeded(string Json)
{
	public static ToolSucceeded Serialize<T>(T response, JsonTypeInfo<T> typeInfo) => new(JsonSerializer.Serialize(response, typeInfo));
}

/// <summary>A call the tool declines, such as invalid arguments or nothing found; the client receives it as an <see cref="ErrorResponse"/>.</summary>
public sealed record ToolRejected(string ErrorType, string Message);

/// <summary>Runs a tool handler with the span, logs and error handling every MCP tool shares.</summary>
public static class McpToolRunner
{
	/// <param name="toolNameTemplate">The tool's <c>[McpToolName]</c> template, e.g. <c>search_{resource}</c>.</param>
	public static async Task<string> RunAsync(
		ILogger logger,
		string toolNameTemplate,
		IReadOnlyDictionary<string, object?> arguments,
		Func<Task<ToolReply>> handle
	)
	{
		var toolName = McpToolTelemetry.ResolveToolName(toolNameTemplate);
		using var activity = McpToolTelemetry.StartActivity(toolName);
		McpToolTelemetry.LogStart(logger, toolName, McpToolTelemetry.SetPayloadMetadata(activity, arguments));
		var duration = Stopwatch.StartNew();
		var outcome = "failure";

		try
		{
			switch (await handle())
			{
				case ToolSucceeded succeeded:
					McpToolTelemetry.MarkSuccess(activity);
					outcome = "success";
					return succeeded.Json;
				case ToolRejected rejected:
					McpToolTelemetry.MarkFailure(activity, rejected.ErrorType, rejected.Message);
					return JsonSerializer.Serialize(new ErrorResponse(rejected.Message), McpJsonContext.Default.ErrorResponse);
			}
		}
		catch (OperationCanceledException)
		{
			McpToolTelemetry.MarkCancelled(activity);
			outcome = "cancelled";
			throw;
		}
		catch (SearchUnavailableException ex)
		{
			// Transient backend failure: McpException gives the client IsError=true so it knows to retry.
			McpToolTelemetry.MarkFailure(activity, ex);
			outcome = "unavailable";
			throw new McpException(ex.Message, ex);
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			McpToolTelemetry.MarkFailure(activity, ex);
			logger.LogError(ex, "{ToolName} failed for {Arguments}", toolName, arguments);
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
		finally
		{
			duration.Stop();
			McpToolTelemetry.LogCompletion(logger, toolName, duration.ElapsedMilliseconds, outcome);
		}
		throw new UnreachableException();
	}
}
