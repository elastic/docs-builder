// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// Every public operation of <see cref="Widget"/> against every rule it declares, one generated test per kind of rule. This is how an aggregate's
/// operations are verified: an operation that can open a state a rule forbids fails with the rule, the operation and the smallest input.
/// </summary>
public class WidgetAutoTests
{
	private static readonly Gen<Widget> Widgets = Gen.Select(
		Gen.String.AlphaNumeric.Where(s => s.Length > 0).Select(s => "  " + s),
		Gen.Int[1, WidgetConstraints.MaxSize]
	).Select(t => new Widget(t.Item1, t.Item2));

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest.For(WidgetConstraints.Instance, Widgets).Seed(RunSeed.Value).AsDataSource();

	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	[Test]
	public void Actions_AreThePublicMethodsReturningTheRecord() =>
		WidgetConstraints.Actions.Select(a => a.Name).Should().BeEquivalentTo(["Grow", "Rename", "Tidy"]);
}
