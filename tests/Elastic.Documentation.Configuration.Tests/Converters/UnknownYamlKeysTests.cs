// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Converters;

namespace Elastic.Documentation.Configuration.Tests.Converters;

public class UnknownYamlKeysTests
{
	[Test]
	public void Of_ReturnsTheWrittenKeysThatAreNotKnown_InWrittenOrder() =>
		UnknownYamlKeys.Of(["b", "known", "a", "also-known", "c"], "known", "also-known").Should().Equal("b", "a", "c");

	[Test]
	public void Of_IsCaseSensitive() => UnknownYamlKeys.Of(["Known"], "known").Should().Equal("Known");

	[Test]
	public void Of_NothingUnknown_IsEmpty() => UnknownYamlKeys.Of(["a"], "a").Should().BeEmpty();
}
