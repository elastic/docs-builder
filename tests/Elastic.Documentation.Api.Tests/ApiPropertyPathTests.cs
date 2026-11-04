// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Api.Tests;

public class ApiPropertyPathTests
{
	[Test]
	[Arguments("[].retriever.standard", "retriever.standard")]
	[Arguments("labels.<string>.value", "labels.value")]
	[Arguments("query.bool.must[].match", "query.bool.must.match")]
	[Arguments("retriever", "retriever")]
	public void ParentKey_Markers_AreRemoved(string path, string expected) => ApiPropertyPath.ParentKey(path).Should().Be(expected);
}
