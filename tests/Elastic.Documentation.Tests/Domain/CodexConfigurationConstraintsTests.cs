// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Codex;

namespace Elastic.Documentation.Tests.Domain;

public class CodexConfigurationConstraintsTests
{
	/// <summary>
	/// The read stage is empty on purpose: main accepts every codex file, so a rule here would add a finding. A rule belongs in a behaviour PR that
	/// also shows the snapshot diff; that PR replaces this test with the rule's own.
	/// </summary>
	[Test]
	public void ReadStage_IsIntentionallyEmpty() => CodexConfiguration.Constraints.AsConstraints().Should().BeEmpty();
}
