// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Backfill;

namespace Elastic.Changelog.Tests.Backfill;

public class CanonicalJsonTests
{
	[Fact]
	public void Canonicalize_SortsObjectKeysByOrdinal()
	{
		var canonical = CanonicalJson.Canonicalize(/*lang=json,strict*/ """{"b":1,"a":2,"Z":3}""");

		// Ordinal order puts upper-case 'Z' before lower-case letters.
		canonical.Should().Be(/*lang=json,strict*/ """{"Z":3,"a":2,"b":1}""");
	}

	[Fact]
	public void Canonicalize_RemovesInsignificantWhitespace()
	{
		var canonical = CanonicalJson.Canonicalize(/*lang=json,strict*/ "{\n  \"a\" : [ 1 , 2 ]\n}");

		canonical.Should().Be(/*lang=json,strict*/ """{"a":[1,2]}""");
	}

	[Fact]
	public void Canonicalize_DropsNullObjectProperties()
	{
		var canonical = CanonicalJson.Canonicalize(/*lang=json,strict*/ """{"a":1,"b":null}""");

		canonical.Should().Be(/*lang=json,strict*/ """{"a":1}""");
	}

	[Fact]
	public void Canonicalize_KeepsNullArrayItems()
	{
		var canonical = CanonicalJson.Canonicalize(/*lang=json,strict*/ """{"a":[1,null,2]}""");

		canonical.Should().Be(/*lang=json,strict*/ """{"a":[1,null,2]}""");
	}

	[Fact]
	public void Canonicalize_PreservesArrayOrder()
	{
		var canonical = CanonicalJson.Canonicalize(/*lang=json,strict*/ """{"a":[3,1,2]}""");

		canonical.Should().Be(/*lang=json,strict*/ """{"a":[3,1,2]}""");
	}

	[Fact]
	public void Canonicalize_NormalizesLineEndingsInsideStrings()
	{
		var canonical = CanonicalJson.Canonicalize(/*lang=json,strict*/ """{"text":"line one\r\nline two\rline three"}""");

		canonical.Should().Be(/*lang=json,strict*/ """{"text":"line one\nline two\nline three"}""");
	}

	[Fact]
	public void Canonicalize_SortsNestedObjectsToo()
	{
		var canonical = CanonicalJson.Canonicalize(/*lang=json,strict*/ """{"outer":{"b":1,"a":{"d":4,"c":3}}}""");

		canonical.Should().Be(/*lang=json,strict*/ """{"outer":{"a":{"c":3,"d":4},"b":1}}""");
	}

	[Fact]
	public void Canonicalize_KeepsNumberTextVerbatim()
	{
		var canonical = CanonicalJson.Canonicalize(/*lang=json,strict*/ """{"a":10,"b":0}""");

		canonical.Should().Be(/*lang=json,strict*/ """{"a":10,"b":0}""");
	}

	[Fact]
	public void Canonicalize_DuplicateKeys_FailsWithClearError()
	{
		var act = () => CanonicalJson.Canonicalize(/*lang=json,strict*/ """{"a":1,"a":2}""");

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*'a' appears more than once*");
	}

	[Fact]
	public void Canonicalize_InvalidJson_FailsWithClearError()
	{
		var act = () => CanonicalJson.Canonicalize("not json at all");

		act.Should().Throw<BackfillDocumentException>()
			.WithMessage("*not valid JSON*");
	}

	[Fact]
	public void Canonicalize_IsIdempotent()
	{
		const string input = /*lang=json,strict*/ """{"b":{"y":2,"x":1},"a":[true,false,null]}""";

		var once = CanonicalJson.Canonicalize(input);
		var twice = CanonicalJson.Canonicalize(once);

		twice.Should().Be(once);
	}
}
