// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Documentation.Builder.Http;
using Nullean.Entities;
using Nullean.Entities.CsCheck;
using Nullean.Snapshots;

namespace Documentation.Builder.Tests;

public class BuildStatusMachineTests
{
	/// <summary>
	/// Law: every state of the declared machine is reachable and every edge can be taken.
	/// Consequence if broken: the diagram documents a state or a move nothing can make.
	/// </summary>
	[Test]
	public void TheMachineIsSound() => BuildStatusMachine.Machine.Spec().Exhaustive();

	/// <summary>
	/// Law: the diagram the declaration draws is the one pinned here.
	/// Consequence if broken: a state, a trigger or a target changed without anyone reading the new diagram.
	/// </summary>
	[Test]
	public void TheDiagramGeneratedFromTheDeclaration_IsPinned() =>
		BuildStatusMachine
			.Machine
			.ToMermaid()
			.Should()
			.MatchInlineSnapshot(
				"""
		stateDiagram-v2
		    [*] --> Idle
		    Idle --> Building : Start
		    Complete --> Building : Start
		    Building --> Complete : Finish
		    Building --> Complete : Fail
		    Building --> Idle : Interrupt
		"""
			);

	/// <summary>
	/// Law: after every trigger the real service is where the machine says: the next state when the machine allows it, the same state when it refuses.
	/// The service is driven through every walk of the machine, with a build the test holds open, ended, thrown from, or interrupted by the host stopping.
	/// Consequence if broken: the status endpoint (and the editor panel) shows a state the build cannot be in.
	/// </summary>
	[Test]
	public Task TheServiceFollowsItsStatusMachine() =>
		BuildStatusMachine.Machine.ConformToServiceAsync(
			() => new BuildHarness(new ManualTimeProvider()),
			h => h.Status,
			iter: 60,
			bindings: [
				("Start", h => h.StartAsync(TestToken)),
				("Finish", h => h.EndBuildAsync(TestToken)),
				("Fail", h => h.FailAsync(TestToken)),
				("Interrupt", h => h.InterruptAsync(TestToken))
			]
		);

	private static Cancel TestToken => TestContext.Current?.Execution.CancellationToken ?? Cancel.None;
}
