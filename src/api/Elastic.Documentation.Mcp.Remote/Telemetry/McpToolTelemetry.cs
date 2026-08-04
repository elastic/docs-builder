// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Mcp.Remote.Telemetry;

public static class McpToolTelemetry
{
	internal const string McpToolSourceName = "Elastic.Documentation.Api.McpTools";
	internal const string McpMeterName = "Elastic.Documentation.Api.McpTools";

	private static readonly ActivitySource McpActivitySource = new(McpToolSourceName);
	private static readonly Meter McpMeter = new(McpMeterName);
	private static readonly Counter<long> ToolCallsCounter =
		McpMeter.CreateCounter<long>("mcp.tool.calls", unit: "{call}", description: "Number of MCP tool calls");
	private static readonly Histogram<double> ToolDurationHistogram =
		McpMeter.CreateHistogram<double>("mcp.tool.duration", unit: "s", description: "Duration of MCP tool calls in seconds");

	private static readonly McpServerProfile ServerProfile = ResolveServerProfile();
	private static readonly string? ServerVersion = ResolveServerVersion();

	private const string McpMethodToolsCall = "tools/call";

	public static string ResolveToolName(string template) =>
		template
			.Replace("{resource}", ServerProfile.ResourceNoun, StringComparison.Ordinal)
			.Replace("{scope}", ServerProfile.ScopePrefix, StringComparison.Ordinal);

	public static Activity? StartActivity(string toolName)
	{
		// Rename the ASP.NET Core server span so each tool call appears as its own
		// transaction in Elastic Observability (e.g. "tools/call search_docs").
		// The server activity is already Current before we create our child span.
		EnrichServerActivity(toolName);

		var activity = McpActivitySource.StartActivity($"mcp.tool.{toolName}", ActivityKind.Internal);
		_ = activity?.SetTag("mcp.method.name", McpMethodToolsCall);
		_ = activity?.SetTag("mcp.tool.name", toolName);
		_ = activity?.SetTag("mcp.server.profile", ServerProfile.Name);
		if (!string.IsNullOrWhiteSpace(ServerVersion))
			_ = activity?.SetTag("mcp.server.version", ServerVersion);
		return activity;
	}

	// Find the nearest ancestor (or current) activity with ActivityKind.Server and
	// rename it to "tools/call {toolName}" with the standard MCP semconv attributes.
	private static void EnrichServerActivity(string toolName)
	{
		var serverActivity = FindServerActivity();
		if (serverActivity is null)
			return;

		// Set tags now so McpSpanRenameProcessor can read them in OnEnd to rename
		// the transaction. DisplayName cannot be set here because ASP.NET Core
		// instrumentation overwrites it with the route template in OnStopActivity,
		// which runs after the tool but before processor OnEnd callbacks.
		_ = serverActivity.SetTag("mcp.method.name", McpMethodToolsCall);
		_ = serverActivity.SetTag("mcp.tool.name", toolName);
	}

	private static Activity? FindServerActivity()
	{
		var current = Activity.Current;
		while (current is not null)
		{
			if (current.Kind == ActivityKind.Server)
				return current;
			current = current.Parent;
		}
		return null;
	}

	public static PayloadMetadata SetPayloadMetadata(Activity? activity, IReadOnlyDictionary<string, object?> arguments)
	{
		var argumentKeys = arguments.Keys
			.OrderBy(k => k, StringComparer.Ordinal)
			.ToArray();
		var argKeys = string.Join(",", argumentKeys);

		_ = activity?.SetTag("mcp.payload.arg_count", argumentKeys.Length);
		_ = activity?.SetTag("mcp.payload.arg_keys", argKeys);

		foreach (var kvp in arguments.Where(kvp => kvp.Value is string))
		{
			if (kvp.Value is string value)
				_ = activity?.SetTag($"mcp.payload.{kvp.Key}.length", value.Length);
		}

		return new PayloadMetadata(argumentKeys.Length, argKeys);
	}

	public static void MarkSuccess(Activity? activity)
	{
		_ = activity?.SetTag("mcp.call.success", true);
		_ = activity?.SetStatus(ActivityStatusCode.Ok);
	}

	public static void MarkFailure(Activity? activity, Exception ex)
	{
		_ = activity?.SetTag("mcp.call.success", false);
		_ = activity?.SetTag("mcp.call.error_type", ex.GetType().FullName);
		_ = activity?.SetTag("error.message", ex.Message);
		_ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
	}

	public static void MarkFailure(Activity? activity, string errorType, string message)
	{
		_ = activity?.SetTag("mcp.call.success", false);
		_ = activity?.SetTag("mcp.call.error_type", errorType);
		_ = activity?.SetTag("error.message", message);
		_ = activity?.SetStatus(ActivityStatusCode.Error, message);
	}

	public static void MarkCancelled(Activity? activity)
	{
		_ = activity?.SetTag("mcp.call.success", false);
		_ = activity?.SetTag("mcp.call.cancelled", true);
		_ = activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
	}

	public static void LogStart(ILogger logger, string toolName, PayloadMetadata metadata) =>
		logger.LogInformation(
			"MCP tool call started {ToolName} (profile={Profile}, arg_count={ArgCount}, arg_keys={ArgKeys})",
			toolName,
			ServerProfile.Name,
			metadata.ArgCount,
			metadata.ArgKeys);

	public static void LogCompletion(ILogger logger, string toolName, long durationMs, string outcome)
	{
		logger.LogInformation(
			"MCP tool call completed {ToolName} (profile={Profile}, duration_ms={DurationMs}, outcome={Outcome})",
			toolName,
			ServerProfile.Name,
			durationMs,
			outcome);

		var tags = new TagList
		{
			{ "mcp.tool.name", toolName },
			{ "mcp.method.name", McpMethodToolsCall },
			{ "mcp.server.profile", ServerProfile.Name },
			{ "outcome", outcome }
		};
		ToolCallsCounter.Add(1, tags);
		ToolDurationHistogram.Record(durationMs / 1000.0, tags);
	}

	private static McpServerProfile ResolveServerProfile()
	{
		var configuredProfile = SystemEnvironmentVariables.Instance.McpServerProfile;
		return McpServerProfile.Resolve(configuredProfile);
	}

	private static string? ResolveServerVersion()
	{
		var informationalVersion = Assembly.GetExecutingAssembly()
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

		return informationalVersion?.Split(['+', '-'])[0];
	}
}

public readonly record struct PayloadMetadata(int ArgCount, string ArgKeys);
