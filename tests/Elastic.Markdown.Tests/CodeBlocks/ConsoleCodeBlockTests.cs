// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Tests.Inline;
using FluentAssertions;
using JetBrains.Annotations;

namespace Elastic.Markdown.Tests.CodeBlocks;

public abstract class ConsoleCodeBlockTests(
	ITestOutputHelper output,
	[LanguageInjection("markdown")] string markdown
)
	: BlockTest<EnhancedCodeBlock>(output, markdown)
{
	[Fact]
	public void ParsesConsoleCodeBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsLanguage() => Block!.Language.Should().Be("json");
}

public class SingleConsoleApiCallTests(ITestOutputHelper output) : ConsoleCodeBlockTests(output,
"""
```console
GET /mydocuments/_search
{
    "from": 1,
    "query": {
        "match_all" {}
    }
}
```
"""
)
{
	[Fact]
	public void CreatesSingleApiSegment()
	{
		Block!.ApiSegments.Should().HaveCount(1);
		var segment = Block.ApiSegments[0];
		segment.Header.Should().Be("GET /mydocuments/_search");
		segment.ContentLines.Should().HaveCount(6);
		segment.ContentLines[0].Should().Be("{");
		segment.ContentLines[1].Should().Be("    \"from\": 1,");
		segment.ContentLines[2].Should().Be("    \"query\": {");
		segment.ContentLines[3].Should().Be("        \"match_all\" {}");
		segment.ContentLines[4].Should().Be("    }");
		segment.ContentLines[5].Should().Be("}");
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

public class MultipleConsoleApiCallsTests(ITestOutputHelper output) : ConsoleCodeBlockTests(output,
"""
```console
GET /mydocuments/_search
{
    "from": 1,
    "query": {
        "match_all" {}
    }
}

POST /mydocuments/_doc
{
    "title": "New Document",
    "content": "This is a sample document"
}
```
"""
)
{
	[Fact]
	public void CreatesMultipleApiSegments()
	{
		Block!.ApiSegments.Should().HaveCount(2);

		// First segment
		var firstSegment = Block.ApiSegments[0];
		firstSegment.Header.Should().Be("GET /mydocuments/_search");
		firstSegment.ContentLines.Should().HaveCount(6);
		firstSegment.ContentLines[0].Should().Be("{");
		firstSegment.ContentLines[1].Should().Be("    \"from\": 1,");
		firstSegment.ContentLines[2].Should().Be("    \"query\": {");
		firstSegment.ContentLines[3].Should().Be("        \"match_all\" {}");
		firstSegment.ContentLines[4].Should().Be("    }");
		firstSegment.ContentLines[5].Should().Be("}");

		// Second segment
		var secondSegment = Block.ApiSegments[1];
		secondSegment.Header.Should().Be("POST /mydocuments/_doc");
		secondSegment.ContentLines.Should().HaveCount(4);
		secondSegment.ContentLines[0].Should().Be("{");
		secondSegment.ContentLines[1].Should().Be("    \"title\": \"New Document\",");
		secondSegment.ContentLines[2].Should().Be("    \"content\": \"This is a sample document\"");
		secondSegment.ContentLines[3].Should().Be("}");
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

public class ConsoleWithDifferentHttpVerbsTests(ITestOutputHelper output) : ConsoleCodeBlockTests(output,
"""
```console
GET /api/users
{
    "size": 10
}

PUT /api/users/123
{
    "name": "John Doe",
    "email": "john@example.com"
}

DELETE /api/users/123
```
"""
)
{
	[Fact]
	public void HandlesDifferentHttpVerbs()
	{
		Block!.ApiSegments.Should().HaveCount(3);

		Block.ApiSegments[0].Header.Should().Be("GET /api/users");
		Block.ApiSegments[1].Header.Should().Be("PUT /api/users/123");
		Block.ApiSegments[2].Header.Should().Be("DELETE /api/users/123");
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

public class ConsoleWithCalloutsTests(ITestOutputHelper output) : ConsoleCodeBlockTests(output,
"""
```console
GET /mydocuments/_search
{
    "from": 1,
    "query": {
        "match_all" {} <1>
    }
}

POST /mydocuments/_doc
{
    "title": "New Document" <2>
}
```

1. This query matches all documents
2. The document title
"""
)
{
	[Fact]
	public void CreatesMultipleApiSegmentsWithCallouts()
	{
		Block!.ApiSegments.Should().HaveCount(2);
		Block.CallOuts.Should().HaveCount(2);
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

public class ConsoleWithEmptyLinesTests(ITestOutputHelper output) : ConsoleCodeBlockTests(output,
"""
```console
GET /api/test
{
    "param": "value"
}

POST /api/test
{
    "another": "value"
}
```
"""
)
{
	[Fact]
	public void HandlesEmptyLinesBetweenApiCalls()
	{
		Block!.ApiSegments.Should().HaveCount(2);
		Block.ApiSegments[0].Header.Should().Be("GET /api/test");
		Block.ApiSegments[1].Header.Should().Be("POST /api/test");
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

public class ConsoleWithOnlyHeadersTests(ITestOutputHelper output) : ConsoleCodeBlockTests(output,
"""
```console
GET /api/health
POST /api/status
DELETE /api/cleanup
```
"""
)
{
	[Fact]
	public void HandlesApiCallsWithoutBodies()
	{
		Block!.ApiSegments.Should().HaveCount(3);
		Block.ApiSegments[0].Header.Should().Be("GET /api/health");
		Block.ApiSegments[1].Header.Should().Be("POST /api/status");
		Block.ApiSegments[2].Header.Should().Be("DELETE /api/cleanup");

		Block.ApiSegments.Should().OnlyContain(s => s.ContentLines.Count == 0);
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

public class ConsoleWithCalloutsOnHttpVerbsTests(ITestOutputHelper output) : ConsoleCodeBlockTests(output,
"""
```console
GET /api/users <1>
{
    "size": 10
}

POST /api/users <2>
{
    "name": "John Doe"
}
```

1. Get all users
2. Create a new user
"""
)
{
	[Fact]
	public void CreatesMultipleApiSegmentsWithCalloutsOnHttpVerbs()
	{
		Block!.ApiSegments.Should().HaveCount(2);
		Block.CallOuts.Should().HaveCount(2);
	}

	[Fact]
	public void RendersCalloutsInHttpVerbHeaders()
	{
		Block!.ApiSegments.Should().HaveCount(2);
		Block.ApiSegments[0].Header.Should().Be("GET /api/users");
		Block.ApiSegments[1].Header.Should().Be("POST /api/users");
		Block.CallOuts.Should().HaveCount(2);
	}

	[Fact]
	public void RendersCalloutHtmlInConsoleCodeBlocks()
	{
		var viewModel = new CodeViewModel
		{
			ApiSegments = Block!.ApiSegments,
			Language = Block.Language,
			Caption = null,
			CrossReferenceName = null,
			RawIncludedFileContents = null,
			EnhancedCodeBlock = Block
		};

		var calloutHtml = viewModel.RenderLineWithCallouts(Block.ApiSegments[0].Header, Block.ApiSegments[0].LineNumber);
		calloutHtml.Value.Should().Contain("code-callout");
		calloutHtml.Value.Should().Contain("data-index=\"1\"");
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

public class ConsoleWithCalloutsInJsonContentTests(ITestOutputHelper output) : ConsoleCodeBlockTests(output,
"""
```console
PUT my-index-000001
{
  "mappings": {
    "enabled": false <1>
  }
}

PUT my-index-000001/_doc/session_1
{
  "user_id": "kimchy",
  "session_data": {
    "arbitrary_object": {
      "some_array": [ "foo", "bar", { "baz": 2 } ]
    }
  },
  "last_updated": "2015-12-06T18:20:22"
}

GET my-index-000001/_doc/session_1 <2>

GET my-index-000001/_mapping <3>
```

1. The entire mapping is disabled.
2. The document can be retrieved.
3. Checking the mapping reveals that no fields have been added.
"""
)
{
	[Fact]
	public void CreatesMultipleApiSegmentsWithCalloutsInJsonContent()
	{
		Block!.ApiSegments.Should().HaveCount(4);
		Block.CallOuts.Should().HaveCount(3);
	}

	[Fact]
	public void RendersCalloutsInJsonContent()
	{
		// Test that callouts in JSON content are properly rendered
		var viewModel = new CodeViewModel
		{
			ApiSegments = Block!.ApiSegments,
			Language = Block.Language,
			Caption = null,
			CrossReferenceName = null,
			RawIncludedFileContents = null,
			EnhancedCodeBlock = Block
		};

		// The first segment should have callouts in its JSON content
		var firstSegment = Block.ApiSegments[0];
		var contentHtml = viewModel.RenderContentLinesWithCallouts(firstSegment.ContentLinesWithNumbers);
		contentHtml.Value.Should().Contain("code-callout");
		contentHtml.Value.Should().Contain("data-index=\"1\"");
		contentHtml.Value.Should().NotContain("<1>");
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

