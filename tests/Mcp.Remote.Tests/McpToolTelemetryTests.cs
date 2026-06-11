// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Mcp.Remote;
using Elastic.Documentation.Mcp.Remote.Telemetry;
using AwesomeAssertions;

namespace Mcp.Remote.Tests;

public class McpToolTelemetryTests
{
	[Fact]
	public void ResolveToolName_UsesProfilePlaceholders()
	{
		var template = "find_{scope}related_{resource}";
		var profile = McpServerProfile.Resolve(SystemEnvironmentVariables.Instance.McpServerProfile);
		var expected = template
			.Replace("{resource}", profile.ResourceNoun, StringComparison.Ordinal)
			.Replace("{scope}", profile.ScopePrefix, StringComparison.Ordinal);

		var resolved = McpToolTelemetry.ResolveToolName(template);

		resolved.Should().Be(expected);
	}

	[Fact]
	public void SetPayloadMetadata_SetsArgCountKeysAndStringLengths()
	{
		using var listener = CreateListener();
		using var activity = McpToolTelemetry.StartActivity("test_tool");
		activity.Should().NotBeNull();

		var metadata = McpToolTelemetry.SetPayloadMetadata(activity, new Dictionary<string, object?>
		{
			["query"] = "cluster setup",
			["pageNumber"] = 2,
			["topic"] = "observability"
		});

		metadata.ArgCount.Should().Be(3);
		metadata.ArgKeys.Should().Be("pageNumber,query,topic");

		var tags = activity!.TagObjects.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		tags["mcp.payload.arg_count"].Should().Be(3);
		tags["mcp.payload.arg_keys"].Should().Be("pageNumber,query,topic");
		tags["mcp.payload.query.length"].Should().Be(13);
		tags["mcp.payload.topic.length"].Should().Be(13);
		tags.ContainsKey("mcp.payload.pageNumber.length").Should().BeFalse();
	}

	[Fact]
	public void StartActivity_UsesInternalKind()
	{
		using var listener = CreateListener();
		using var activity = McpToolTelemetry.StartActivity("test_tool");

		activity.Should().NotBeNull();
		activity!.Kind.Should().Be(ActivityKind.Internal);
	}

	[Fact]
	public void MarkSuccess_SetsSuccessTagAndOkStatus()
	{
		using var listener = CreateListener();
		using var activity = McpToolTelemetry.StartActivity("test_tool");
		activity.Should().NotBeNull();

		McpToolTelemetry.MarkSuccess(activity);

		var tags = activity!.TagObjects.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		tags["mcp.call.success"].Should().Be(true);
		activity.Status.Should().Be(ActivityStatusCode.Ok);
	}

	[Fact]
	public void MarkFailure_SetsFailureTagsAndErrorStatus()
	{
		using var listener = CreateListener();
		using var activity = McpToolTelemetry.StartActivity("test_tool");
		activity.Should().NotBeNull();

		var error = new InvalidOperationException("gateway failed");
		McpToolTelemetry.MarkFailure(activity, error);

		var tags = activity!.TagObjects.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		tags["mcp.call.success"].Should().Be(false);
		tags["mcp.call.error_type"].Should().Be(typeof(InvalidOperationException).FullName);
		tags["error.message"].Should().Be("gateway failed");
		activity.Status.Should().Be(ActivityStatusCode.Error);
		activity.StatusDescription.Should().Be("gateway failed");
		activity.Events.Should().Contain(e => e.Name == "exception");
	}

	[Fact]
	public void MarkFailure_PropagatesErrorStatusToServerAncestor()
	{
		using var serverSource = new ActivitySource("Test.McpServer");
		using var listener = CreateListenerForSources(McpToolTelemetry.McpToolSourceName, "Test.McpServer");

		// Simulate the ASP.NET Core server span (the HTTP transaction in APM).
		using var serverActivity = serverSource.StartActivity("POST /mcp", ActivityKind.Server);
		serverActivity.Should().NotBeNull();

		// Tool span is a child of the server span (matches runtime Activity parenting).
		using var toolActivity = McpToolTelemetry.StartActivity("test_tool");
		toolActivity.Should().NotBeNull();
		toolActivity!.Parent.Should().Be(serverActivity);

		var ex = new InvalidOperationException("ES is down");
		McpToolTelemetry.MarkFailure(toolActivity, ex);

		// Tool span carries the error and exception event.
		toolActivity.Status.Should().Be(ActivityStatusCode.Error);
		toolActivity.Events.Should().Contain(e => e.Name == "exception");

		// Server/transaction span is also marked Error — this makes the HTTP transaction
		// appear as a failing transaction in APM despite the 200 response code.
		serverActivity!.Status.Should().Be(ActivityStatusCode.Error);
		serverActivity.StatusDescription.Should().Be("ES is down");
		serverActivity.Events.Should().Contain(e => e.Name == "exception");
	}

	[Fact]
	public void MarkFailure_NoServerAncestor_DoesNotThrow()
	{
		// In unit tests (and any context without an active server span) FailServerSpan
		// should be a no-op rather than failing.
		using var listener = CreateListener();
		using var activity = McpToolTelemetry.StartActivity("test_tool");
		activity.Should().NotBeNull();

		var ex = new InvalidOperationException("isolated failure");
		var act = () => McpToolTelemetry.MarkFailure(activity, ex);

		act.Should().NotThrow();
		activity!.Status.Should().Be(ActivityStatusCode.Error);
	}

	[Fact]
	public void MarkCancelled_SetsCancelledTagAndErrorStatus()
	{
		using var listener = CreateListener();
		using var activity = McpToolTelemetry.StartActivity("test_tool");
		activity.Should().NotBeNull();

		McpToolTelemetry.MarkCancelled(activity);

		var tags = activity!.TagObjects.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		tags["mcp.call.success"].Should().Be(false);
		tags["mcp.call.cancelled"].Should().Be(true);
		activity.Status.Should().Be(ActivityStatusCode.Error);
		activity.StatusDescription.Should().Be("cancelled");
	}

	private static ActivityListener CreateListener() =>
		CreateListenerForSources(McpToolTelemetry.McpToolSourceName);

	private static ActivityListener CreateListenerForSources(params string[] sourceNames)
	{
		var nameSet = new HashSet<string>(sourceNames, StringComparer.Ordinal);
		var listener = new ActivityListener
		{
			ShouldListenTo = source => nameSet.Contains(source.Name),
			Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
		};
		ActivitySource.AddActivityListener(listener);
		return listener;
	}
}
