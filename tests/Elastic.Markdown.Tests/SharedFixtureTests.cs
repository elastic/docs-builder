// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using AwesomeAssertions;
using Elastic.Markdown.IO;
using Elastic.Markdown.Tests.Directives;
using Elastic.Markdown.Tests.Inline;

namespace Elastic.Markdown.Tests;

// `InlineTest` and `DirectiveTest` build the documentation set once per test class. If that stops being the case every test
// builds its own set again, which is several times slower, so these fail when a class sees more than one set.
public class DirectiveFixtureIsBuiltOncePerClass() : DirectiveTest("Some text.")
{
	private static readonly ConcurrentDictionary<DocumentationSet, byte> SeenSets = new();

	[Test]
	public void FirstTestSeesTheSharedSet() => AssertOneSet();

	[Test]
	public void SecondTestSeesTheSharedSet() => AssertOneSet();

	[Test]
	public void ThirdTestSeesTheSharedSet() => AssertOneSet();

	private void AssertOneSet()
	{
		_ = SeenSets.TryAdd(Set, 0);
		SeenSets.Count.Should().Be(1);
	}
}

public class InlineFixtureIsBuiltOncePerClass() : InlineTest("Some text.")
{
	private static readonly ConcurrentDictionary<DocumentationSet, byte> SeenSets = new();

	[Test]
	public void FirstTestSeesTheSharedSet() => AssertOneSet();

	[Test]
	public void SecondTestSeesTheSharedSet() => AssertOneSet();

	[Test]
	public void ThirdTestSeesTheSharedSet() => AssertOneSet();

	private void AssertOneSet()
	{
		_ = SeenSets.TryAdd(Set, 0);
		SeenSets.Count.Should().Be(1);
	}
}
