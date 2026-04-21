// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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

	private static ActivityListener CreateListener()
	{
		var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == "Elastic.Documentation.Api.McpTools",
			Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
		};

		ActivitySource.AddActivityListener(listener);
		return listener;
	}
}
