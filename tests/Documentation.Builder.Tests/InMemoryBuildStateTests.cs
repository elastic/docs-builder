// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CsCheck;
using Documentation.Builder.Http;
using Elastic.Documentation.Tests.Domain;
using Nullean.Entities.CsCheck;

namespace Documentation.Builder.Tests;

/// <summary>
/// The background validation build behind <c>docs-builder serve</c>, driven by random saves with random gaps between them. The spec is
/// <see cref="BuildEventSpec"/> and <see cref="BuildStateConstraints"/>, declared next to the service; the tests only say how to drive it.
/// </summary>
public class InMemoryBuildStateTests
{
	// a build reports up to a handful of findings and takes up to 20 minutes of virtual time
	private static Gen<BuildPlan> Plans { get; } = Gen.Select(
		Gen.Int[0, 6],
		Gen.Int[0, 20 * 60].Select(seconds => TimeSpan.FromSeconds(seconds)),
		(diagnostics, duration) => new BuildPlan(diagnostics, duration)
	);

	private static Script<BuildHarness> Saves() =>
		Script
			.For<BuildHarness>()
			.Step("Save", Plans, (h, plan) => h.Build(plan))
			.Step("SaveBurst", Gen.Select(Gen.Int[1, 5], Plans, (extra, plan) => (extra, plan)), (h, b) => h.Burst(b.extra, b.plan))
			.Delays(Gen.Int[0, 600_000].Select(ms => TimeSpan.FromMilliseconds(ms)))
			.Length(3, 12);

	/// <summary>
	/// Law: whatever the saves, the broadcast follows the lifecycle (start, findings, end), every build ends, and an end comes within half an hour.
	/// Consequence if broken: the editor panel shows "building" for good, or findings from two builds mix.
	/// </summary>
	[Test]
	public void TheBroadcast_KeepsTheBuildEventSpec() =>
		Saves().SampleWith(time => new BuildHarness(time), h => h.Finish(), BuildEventSpec.All, seed: Props.Seed, iter: 100);

	/// <summary>
	/// Law: the random saves reach what the rules are about: builds start, report findings and end. Not seeded on purpose: a replayed seed is one run,
	/// too small a sample to judge coverage by.
	/// Consequence if broken: the spec above passes because nothing happened.
	/// </summary>
	[Test]
	public void TheSample_ReachesWhatTheRulesAreAbout() =>
		Saves().SampleWith(
			time => new BuildHarness(time),
			h => h.Finish(),
			BuildEventSpec.All,
			new Coverage<BuildEvent>()
				.Expect("a build started", e => e.Type == "build_start", minPercentOfRuns: 95)
				.Expect("a finding was reported", e => e.Type == "diagnostic", minPercentOfRuns: 50)
				.Expect("a build completed", e => e.Type == "build_complete", minPercentOfRuns: 95),
			iter: 100
		);

	/// <summary>
	/// Law: at rest, the status counts add up to the stored findings, and the status is one the panel can draw.
	/// Consequence if broken: the panel's totals disagree with the list under them.
	/// </summary>
	[Test]
	public void AtRest_TheSnapshotKeepsTheStateConstraints() =>
		Saves().SampleState(time => new BuildHarness(time), h => h.Snapshot(), BuildStateConstraints.All, seed: Props.Seed, iter: 100);
}
