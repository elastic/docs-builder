// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Tests.Domain;

public class RepositoryConstraintsTests
{
	/// <summary>
	/// Law: for any repository, the declared law holds: its configuration checkout, taken twice, is the checkout taken once.
	/// Consequence if broken: cloning the configuration repository twice (a retry, a second command) alters it.
	/// </summary>
	[Test]
	public void ConfigurationCheckout_KeepsItsDeclaredLaw() =>
		DomainGen.Of<Repository>().Check(repository => RepositoryConstraints.All.Validate(repository).Should().BeEmpty());
}
