// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Elastic.Documentation.Deploying.Synchronization;

namespace Elastic.Documentation.Deploying.Tests;

/// <summary>
/// The compile-time guard for the one aggregate this assembly owns (see <c>Constraints-Conventions.md</c>): <see cref="SyncPlan"/> has
/// <c>required</c> public members and <c>internal init</c> for the rest, so only this project (<c>SyncPlanLoader</c> and the planners) and the tests make one. System.Text.Json skips a
/// setter it cannot reach, so the second test reads a plan from text and checks every field.
/// </summary>
public class SyncPlanGuardTests
{
	[Test]
	public void SyncPlan_HasOnlyRequiredPublicInitAndNoParameterizedConstructor()
	{
		var type = typeof(SyncPlan);

		type
			.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
			.Where(p => p.SetMethod is { IsPublic: true } && p.GetCustomAttribute<RequiredMemberAttribute>() is null)
			.Should()
			.BeEmpty();
		type.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Where(c => c.GetParameters().Length > 0).Should().BeEmpty();
	}

	[Test]
	public void SyncPlan_IsDeserializedWithEveryField()
	{
		const string json =
			"""
			{
			  "deletion_threshold_default": 0.3,
			  "exclude_patterns": ["a/*", "b/*"],
			  "remote_listing_completed": true,
			  "total_source_files": 7,
			  "total_remote_files": 6,
			  "total_sync_requests": 4,
			  "delete": [ { "destination_path": "gone.html" } ],
			  "add": [ { "local_path": "/out/new.html", "destination_path": "new.html" } ],
			  "update": [ { "local_path": "/out/changed.html", "destination_path": "changed.html" } ],
			  "skip": [ { "local_path": "/out/same.html", "destination_path": "same.html" } ]
			}
			""";

		var result = SyncPlanLoader.Load(json, "plan.json");

		var plan = result.Value!;
		plan.DeleteThresholdDefault.Should().Be(0.3f);
		plan.ExcludePatterns.Should().Equal("a/*", "b/*");
		plan.RemoteListingCompleted.Should().BeTrue();
		plan.TotalSourceFiles.Should().Be(7);
		plan.TotalRemoteFiles.Should().Be(6);
		plan.TotalSyncRequests.Should().Be(4);
		plan.DeleteRequests.Should().ContainSingle().Which.DestinationPath.Should().Be("gone.html");
		plan.AddRequests.Should().ContainSingle().Which.LocalPath.Should().Be("/out/new.html");
		plan.UpdateRequests.Should().ContainSingle().Which.DestinationPath.Should().Be("changed.html");
		plan.SkipRequests.Should().ContainSingle().Which.LocalPath.Should().Be("/out/same.html");
	}

	[Test]
	public void SyncPlan_WithoutARequiredMember_IsAParseError()
	{
		var result = SyncPlanLoader.Load("""{ "exclude_patterns": [] }""", "plan.json");

		result.Value.Should().BeNull();
		result.HasErrors.Should().BeTrue();
	}
}
