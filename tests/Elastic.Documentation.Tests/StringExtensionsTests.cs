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

	[Test]
	[Arguments("fix create visualization", "Fix create visualization")]
	[Arguments("bulk delete observables", "Bulk delete observables")]
	[Arguments("Already capitalized", "Already capitalized")]
	[Arguments("don't return an empty response", "Don't return an empty response")]
	[Arguments("don\u2019t return an empty response", "Don\u2019t return an empty response")]
	[Arguments("kibana.alert.rule is renamed", "kibana.alert.rule is renamed")]
	[Arguments("inputsForVersions is refreshed", "inputsForVersions is refreshed")]
	[Arguments("snake_case_field is renamed", "snake_case_field is renamed")]
	[Arguments("`code` first", "`code` first")]
	[Arguments("elasticsearch projects", "Elasticsearch projects")]
	[Arguments("ml_anomaly", "ml_anomaly")]
	[Arguments("v2 search", "v2 search")]
	[Arguments("", "")]
	public void CapitalizeFirstLetter_CapitalizesPlainWordsAndKeepsCodeLikeNames(string input, string expected) =>
		input.CapitalizeFirstLetter().Should().Be(expected);
}
