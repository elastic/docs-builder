// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Deploying.Synchronization;
using Elastic.Documentation.Tests.Domain;
using NSeverity = Nullean.Entities.Severity;

namespace Elastic.Documentation.Deploying.Tests;

/// <summary>
/// Operation and loader laws of the sync plan, with a fixed seed: the same inputs on every run, a few hundred iterations, shrinking on failure.
/// </summary>
public class SyncPlanProperties
{
	private static Gen<string> Path { get; } = Gen.Select(
		Gen.OneOfConst("a", "b", "docs", "x/y"),
		Gen.Int[0, 99],
		(d, i) => $"{d}/{i}.html"
	);

	private static Gen<IReadOnlyList<DeleteRequest>> Deletes { get; } = Path
		.Select(p => new DeleteRequest { DestinationPath = p })
		.Array[0, 6]
		.Select(a => (IReadOnlyList<DeleteRequest>)a);

	private static Gen<IReadOnlyList<AddRequest>> Adds { get; } = Path
		.Select(p => new AddRequest { LocalPath = $"/out/{p}", DestinationPath = p })
		.Array[0, 6]
		.Select(a => (IReadOnlyList<AddRequest>)a);

	private static Gen<IReadOnlyList<UpdateRequest>> Updates { get; } = Path
		.Select(p => new UpdateRequest { LocalPath = $"/out/{p}", DestinationPath = p })
		.Array[0, 6]
		.Select(a => (IReadOnlyList<UpdateRequest>)a);

	private static Gen<IReadOnlyList<SkipRequest>> Skips { get; } = Path
		.Select(p => new SkipRequest { LocalPath = $"/out/{p}", DestinationPath = p })
		.Array[0, 6]
		.Select(a => (IReadOnlyList<SkipRequest>)a);

	private static Gen<float?> Threshold { get; } = Gen.OneOfConst<float?>(null, 0.1f, 0.2f, 0.9f);

	/// <summary>A plan as the planner states it, over any counts and any lists.</summary>
	private static Gen<SyncPlan> Plans { get; } = Gen.Select(
		Gen.Bool,
		Threshold,
		Gen.Int[0, 5000],
		Gen.Int[0, 5000],
		Deletes,
		Gen.Select(Adds, Updates, Skips),
		(completed, threshold, remote, source, deletes, rest) =>
			SyncPlanLoader.Planned(completed, threshold, ["*.tmp"], remote, source, deletes, rest.Item1, rest.Item2, rest.Item3).Value!
	);

	/// <summary>
	/// Law: the planner's plan keeps every request it was given, in four lists that partition them, states its total as their sum, and carries what
	/// <see cref="SyncPlanConstraints"/> finds: it is always returned, whatever the counts.
	/// Consequence if broken: a deploy plan silently drops a file, or a bad plan throws instead of being reported and refused.
	/// </summary>
	[Test]
	public void Planned_KeepsEveryRequest_AndAgreesWithItsRules() =>
		Gen.Select(Gen.Bool, Threshold, Gen.Int[-5, 5000], Gen.Int[-5, 5000], Deletes, Adds, Updates, Skips).Check(x =>
		{
			var (completed, threshold, remote, source, deletes, adds, updates, skips) = x;

			var result = SyncPlanLoader.Planned(completed, threshold, [], remote, source, deletes, adds, updates, skips);

			var plan = result.Value!;
			plan.DeleteRequests.Should().Equal(deletes);
			plan.AddRequests.Should().Equal(adds);
			plan.UpdateRequests.Should().Equal(updates);
			plan.SkipRequests.Should().Equal(skips);
			plan.TotalSyncRequests.Should().Be(deletes.Count + adds.Count + updates.Count + skips.Count);
			result.HasErrors.Should().Be(SyncPlanConstraints.All.Validate(plan).Any(v => v.Severity == NSeverity.Error));
		});

	/// <summary>
	/// Law: a plan written to a file and loaded again is the plan that was written, with the same verdict.
	/// Consequence if broken: the plan the planner approved is not the plan the deploy applies.
	/// </summary>
	[Test]
	public void Serialize_ThenLoad_RoundTrips() =>
		Plans.Check(plan =>
		{
			var json = SyncPlan.Serialize(plan);

			var loaded = SyncPlanLoader.Load(json, "plan.json");

			loaded.Value.Should().NotBeNull();
			var value = loaded.Value!;
			SyncPlan.Serialize(value).Should().Be(json);
			value.DeleteRequests.Should().Equal(plan.DeleteRequests);
			value.AddRequests.Should().Equal(plan.AddRequests);
			value.UpdateRequests.Should().Equal(plan.UpdateRequests);
			value.SkipRequests.Should().Equal(plan.SkipRequests);
			value.ExcludePatterns.Should().Equal(plan.ExcludePatterns);
			(value.DeleteThresholdDefault, value.RemoteListingCompleted, value.TotalRemoteFiles, value.TotalSourceFiles, value.TotalSyncRequests).Should().Be(
				(plan.DeleteThresholdDefault, plan.RemoteListingCompleted, plan.TotalRemoteFiles, plan.TotalSourceFiles, plan.TotalSyncRequests)
			);
			loaded
				.Diagnostics
				.Select(d => (d.RuleId, d.Severity))
				.Should()
				.Equal(SyncPlanLoader.Check(plan).Diagnostics.Select(d => (d.RuleId, d.Severity)));
		});

	/// <summary>
	/// Law: loading is total: any text at all, truncated, mutated or garbage, is a plan or an error diagnostic, never an exception.
	/// Consequence if broken: a corrupt plan file crashes the deploy with a stack trace instead of a message.
	/// </summary>
	[Test]
	public void Load_OfAnyText_ReturnsAResult() =>
		Gen.Select(Plans, Gen.Int[0, 5], Gen.Double[0, 1], Gen.String).Check(x =>
		{
			var (plan, mutation, at, garbage) = x;
			var json = SyncPlan.Serialize(plan);
			var index = (int)(at * json.Length);
			var text = mutation switch
			{
				0 => json[..index],
				1 => json.Remove(index == json.Length ? 0 : index, 1),
				2 => json.Insert(index, garbage),
				3 => json.Replace("\"add\"", "\"unknown\"", StringComparison.Ordinal),
				4 => json.Replace("[", "{", StringComparison.Ordinal),
				_ => garbage
			};

			var result = SyncPlanLoader.Load(text, "plan.json");

			(result.Value is not null || result.HasErrors).Should().BeTrue("a plan that does not load says why");
		});
}
