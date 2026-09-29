// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using AwesomeAssertions;

namespace Elastic.Documentation.Tests.Domain;

public sealed record CaseA(int Value);

public sealed record CaseB(string Value);

public readonly union SampleUnion(CaseA, CaseB);

/// <summary>
/// Pins the behavior the domain model relies on from C# unions compiled against the net10 polyfill
/// (<c>Polyfills/Unions.cs</c>). If a compiler or SDK update changes any of it, these fail first.
/// </summary>
public class UnionPolyfillTests
{
	private static string Describe(SampleUnion union) => union switch
	{
		CaseA a => $"a:{a.Value}",
		CaseB b => $"b:{b.Value}",
		null => "unset"
	};

	[Test]
	public void Switch_EachCase_MatchesItsCaseType()
	{
		Describe(new CaseA(1)).Should().Be("a:1");
		Describe(new CaseB("x")).Should().Be("b:x");
	}

	[Test]
	public void Switch_DefaultUnion_MatchesNull() => Describe(default).Should().Be("unset");

	[Test]
	public void Value_BoxedThroughIUnion_IsTheCaseInstance()
	{
		object boxed = new SampleUnion(new CaseB("x"));
		boxed.Should().BeAssignableTo<IUnion>().Which.Value.Should().Be(new CaseB("x"));
	}

	[Test]
	public void Equality_SameCaseAndValue_AreEqual() => new SampleUnion(new CaseA(1)).Should().Be(new SampleUnion(new CaseA(1)));

	[Test]
	public void Equality_DifferentCases_AreNotEqual() => new SampleUnion(new CaseA(1)).Should().NotBe(new SampleUnion(new CaseB("1")));
}
