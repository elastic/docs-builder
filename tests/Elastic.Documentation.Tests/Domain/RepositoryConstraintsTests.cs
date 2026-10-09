// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Publishing;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class RepositoryConstraintsTests
{
	private static AutoTest<Repository> Auto() => DomainAutoTest.For(RepositoryConstraints.All);

	public static IEnumerable<Func<AxisCase>> Axes() => Auto().Axes();

	/// <summary>
	/// Law: each axis the repository declares a rule on holds; its configuration checkout, taken twice, is the checkout taken once, and that rule is exercised.
	/// Consequence if broken: cloning the configuration repository twice (a retry, a second command) alters it.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();
}
