// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using OpenTelemetry;

namespace Elastic.Documentation.Mcp.Remote.Telemetry;

/// <summary>
/// Renames the ASP.NET Core server span to "tools/call {tool}" so each MCP tool call
/// appears as its own transaction in Elastic Observability.
///
/// This must run in OnEnd (not OnStart) because ASP.NET Core instrumentation overwrites
/// DisplayName with the matched route template during OnStopActivity, which fires before
/// any processor OnEnd callbacks.
/// </summary>
internal sealed class McpSpanRenameProcessor : BaseProcessor<Activity>
{
	public override void OnEnd(Activity activity)
	{
		if (activity.Kind != ActivityKind.Server)
			return;

		if (activity.GetTagItem("mcp.method.name") is string methodName
			&& activity.GetTagItem("mcp.tool.name") is string toolName)
			activity.DisplayName = $"{methodName} {toolName}";
	}
}
