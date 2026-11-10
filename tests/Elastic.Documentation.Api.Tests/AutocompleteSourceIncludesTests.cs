// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Api.Tests;

public class AutocompleteSourceIncludesTests
{
	/// <summary>
	/// A hit whose <c>_source</c> holds any part of <c>api</c> deserializes into <see cref="ApiOperationMetadata"/>,
	/// which rejects a document missing a required member. The filter must therefore return every required member.
	/// </summary>
	[Test]
	public void AutocompleteSourceIncludes_CoverEveryRequiredApiMember()
	{
		var required = typeof(ApiOperationMetadata)
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.GetCustomAttribute<RequiredMemberAttribute>() is not null)
			.Select(p => $"api.{p.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name}")
			.ToArray();

		required.Should().NotBeEmpty();
		DefaultSearchService<DocumentationDocument>.AutocompleteSourceIncludes.Should().Contain(required);
		DefaultSearchService<DocumentationDocument>.AutocompleteSourceIncludes.Should().Contain("api.routes");
	}

	[Test]
	public void AutocompleteSourceIncludes_ContainTheDeprecatedFlag() =>
		DefaultSearchService<DocumentationDocument>.AutocompleteSourceIncludes.Should().Contain("api.deprecated");

	[Test]
	public void AutocompleteSourceIncludes_ContainTheLandingFlag() =>
		DefaultSearchService<DocumentationDocument>.AutocompleteSourceIncludes.Should().Contain("api.landing");

	[Test]
	public void AutocompleteSourceIncludes_ContainTheProduct() =>
		DefaultSearchService<DocumentationDocument>.AutocompleteSourceIncludes.Should().Contain("product");
}
