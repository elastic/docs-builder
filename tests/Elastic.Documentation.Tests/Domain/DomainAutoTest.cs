// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CsCheck;
using Nullean.Entities;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>The axis tests as this repository runs them: the fixed seed, and one TUnit test per axis a type declares.</summary>
public static class DomainAutoTest
{
	/// <summary>An auto-test over <paramref name="constraints"/> with the shared seed; without <paramref name="valid"/> the values come from <see cref="DomainGen.Of{T}"/>.</summary>
	public static AutoTest<T> For<T>(ConstraintSet<T> constraints, Gen<T>? valid = null) =>
		AutoTest.For(constraints, valid ?? DomainGen.Of<T>()).Seed(Props.Seed);

	/// <summary>The cases as a <c>[MethodDataSource]</c> source: TUnit runs each lazily, so a failing axis fails only its own test.</summary>
	public static IEnumerable<Func<AxisCase>> Axes<T>(this AutoTest<T> auto) => auto.AsDataSource();
}
