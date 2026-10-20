// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Tests;

public class StringExtensionsTests
{
	[Test]
	[Arguments("get-actions-connector-oauth-callback-script", "Get Actions Connector Oauth Callback Script")]
	[Arguments("async_search_delete", "Async Search Delete")]
	[Arguments("GET-Script", "Get Script")]
	[Arguments("-", "")]
	public void TitleCaseFromSlug_KebabOrSnakeCase_ReturnsTitleCasedWords(string slug, string expected) =>
		slug.TitleCaseFromSlug().Should().Be(expected);
}
