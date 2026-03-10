// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Reflection;
using Elastic.Documentation.Api.Core;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Mcp.Remote.Telemetry;

public static class McpToolTelemetry
{
	private static readonly ActivitySource McpActivitySource = new(TelemetryConstants.McpToolSourceName);
	private static readonly McpServerProfile ServerProfile = ResolveServerProfile();
	private static readonly string? ServerVersion = ResolveServerVersion();

	public static string ResolveToolName(string template) =>
		template
			.Replace("{resource}", ServerProfile.ResourceNoun, StringComparison.Ordinal)
			.Replace("{scope}", ServerProfile.ScopePrefix, StringComparison.Ordinal);

	public static Activity? StartActivity(string toolName)
	{
		var activity = McpActivitySource.StartActivity($"mcp.tool.{toolName}", ActivityKind.Server);
		_ = activity?.SetTag("mcp.tool.name", toolName);
		_ = activity?.SetTag("mcp.server.profile", ServerProfile.Name);
		if (!string.IsNullOrWhiteSpace(ServerVersion))
			_ = activity?.SetTag("mcp.server.version", ServerVersion);
		return activity;
	}

	public static PayloadMetadata SetPayloadMetadata(Activity? activity, IReadOnlyDictionary<string, object?> arguments)
	{
		var argumentKeys = arguments.Keys
			.OrderBy(k => k, StringComparer.Ordinal)
			.ToArray();
		var argKeys = string.Join(",", argumentKeys);

		_ = activity?.SetTag("mcp.payload.arg_count", argumentKeys.Length);
		_ = activity?.SetTag("mcp.payload.arg_keys", argKeys);

		foreach (var kvp in arguments)
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

	public static void LogCompletion(ILogger logger, string toolName, long durationMs, string outcome) =>
		logger.LogInformation(
			"MCP tool call completed {ToolName} (profile={Profile}, duration_ms={DurationMs}, outcome={Outcome})",
			toolName,
			ServerProfile.Name,
			durationMs,
			outcome);

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
