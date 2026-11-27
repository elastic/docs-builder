// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Documentation.Builder.Http;

/// <summary>
/// The states of the background validation build behind <c>docs-builder serve</c>, and what moves it between them. The diagram is generated from this
/// declaration (<c>Machine.ToMermaid()</c>), and the tests hold <see cref="InMemoryBuildState"/> to it.
/// </summary>
internal static class BuildStatusMachine
{
	public static StateMachine<BuildStatus, string> Machine { get; } = StateMachine
		.Define<BuildStatus, string>(BuildStatus.Idle)
		// a save starts a build, from rest or after the last one; one already running queues the next and stays as it is
		.Permit("Start", [BuildStatus.Idle, BuildStatus.Complete], BuildStatus.Building)
		.Permit("Finish", BuildStatus.Building, BuildStatus.Complete)
		// a build that throws still ends: the panel reports it complete with the findings it had
		.Permit("Fail", BuildStatus.Building, BuildStatus.Complete)
		// the host stopping mid-build: the build is cancelled and the service is at rest again
		.Permit("Interrupt", BuildStatus.Building, BuildStatus.Idle)
		.Build();
}
