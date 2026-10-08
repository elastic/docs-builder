// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Documentation.Builder.Http;
using Nullean.Entities;
using Nullean.Entities.CsCheck;

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
	/// Law: the diagram in the docs is the one the declaration draws.
	/// Consequence if broken: the documented lifecycle and the declared one drift apart.
	/// </summary>
	[Test]
	public void TheDiagramIsGeneratedFromTheDeclaration()
	{
		var diagram = BuildStatusMachine.Machine.ToMermaid();

		diagram.Should().StartWith("stateDiagram-v2");
		diagram.Should().Contain("Idle").And.Contain("Building").And.Contain("Complete");
		diagram.Should().Contain("Start").And.Contain("Finish").And.Contain("Fail").And.Contain("Interrupt");
	}

	/// <summary>
	/// Law: after every trigger the real service is where the machine says: the next state when the machine allows it, the same state when it refuses.
	/// The service is driven through every walk of the machine, with a build the test holds open, ended, thrown from, or interrupted by the host stopping.
	/// Consequence if broken: the status endpoint (and the editor panel) shows a state the build cannot be in.
	/// </summary>
	[Test]
	public void TheServiceFollowsItsStatusMachine() =>
		BuildStatusMachine
			.Machine
			.ConformToService(
				() => new BuildHarness(new ManualTimeProvider()),
				h => h.Status,
				iter: 60,
				bindings: [
					("Start", h => Run(h.Start)),
					("Finish", h => Run(h.EndBuild)),
					("Fail", h => Run(h.Fail)),
					("Interrupt", h => Run(h.Interrupt))
				]
			)
			.Should()
			.NotBeNull();

	private static Task Run(Action action)
	{
		action();
		return Task.CompletedTask;
	}
}
