// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Tests.Domain;

public class SourcePositionTests
{
	private static Gen<SourcePosition> Positions { get; } = Gen.Select(
		Gen.Int[1, 500],
		Gen.Int[1, 200],
		Gen.Int[1, 500],
		Gen.Int[1, 200]
	).Select(t => new SourcePosition(t.Item1, t.Item2, t.Item3, t.Item4));

	/// <summary>
	/// Law: two positions with the same four coordinates are equal and hash alike, and a position equals only itself when every coordinate matches.
	/// Consequence if broken: records that carry a position compare unequal after loading the same file twice.
	/// </summary>
	[Test]
	public void Equality_IsStructural() =>
		Positions.Check(p =>
		{
			var copy = new SourcePosition(p.Line, p.Column, p.EndLine, p.EndColumn);
			copy.Should().Be(p);
			copy.GetHashCode().Should().Be(p.GetHashCode());
			(p with { EndColumn = p.EndColumn + 1 }).Should().NotBe(p);
		});
}
