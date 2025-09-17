// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Tests.Directives;
using FluentAssertions;

namespace Elastic.Markdown.Tests.FrontMatter;

public class YamlFrontMatterTests(ITestOutputHelper output) : DirectiveTest(output,
"""
---
navigation_title: "Documentation Guide"
sub:
  key: "value"
---

# Elastic Docs v3
"""
)
{
	[Fact]
	public void ReadsTitle() => File.Title.Should().Be("Elastic Docs v3");

	[Fact]
	public void ReadsNavigationTitle() => File.NavigationTitle.Should().Be("Documentation Guide");

	[Fact]
	public void ReadsSubstitutions()
	{
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter!.Properties.Should().NotBeEmpty()
			.And.HaveCount(1)
			.And.ContainKey("key");
	}
}

public class EmptyFileWarnsNeedingATitle(ITestOutputHelper output) : DirectiveTest(output, "")
{
	[Fact]
	public void ReadsTitle() => File.Title.Should().Be("index.md");

	[Fact]
	public void ReadsNavigationTitle() => File.NavigationTitle.Should().Be("index.md");

	[Fact]
	public void WarnsOfNoTitle() =>
		Collector.Diagnostics.Should().NotBeEmpty()
			.And.Contain(d => d.Message.Contains("Document has no title, using file name as title."));
}

public class NavigationTitleSupportReplacements(ITestOutputHelper output) : DirectiveTest(output,
"""
---
title: Elastic Docs v3
navigation_title: "Documentation Guide: {{key}}"
sub:
  key: "value"
---
"""
)
{
	[Fact]
	public void ReadsNavigationTitle() => File.NavigationTitle.Should().Be("Documentation Guide: value");
}

public class ProductsSingle(ITestOutputHelper output) : DirectiveTest(output,
	"""
	---
	products:
	  - id: "apm"
	---

	# APM
	"""
)
{
	[Fact]
	public void ReadsProducts()
	{
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter!.Products.Should().NotBeNull()
			.And.HaveCount(1);
		File.YamlFrontMatter!.Products!.First().Id.Should().Be("apm");
	}
}

public class ProductsMultiple(ITestOutputHelper output) : DirectiveTest(output,
	"""
	---
	products:
	  - id: "apm"
	  - id: "elasticsearch"
	---

	# APM
	"""
)
{
	[Fact]
	public void ReadsProducts()
	{
		File.YamlFrontMatter.Should().NotBeNull();
		File.YamlFrontMatter!.Products.Should().NotBeNull()
			.And.HaveCount(2);
		File.YamlFrontMatter!.Products!.First().Id.Should().Be("apm");
		File.YamlFrontMatter!.Products!.Last().Id.Should().Be("elasticsearch");
	}
}

public class ProductsSuggestionWhenMispelled(ITestOutputHelper output) : DirectiveTest(output,
	"""
	---
	products:
	  - id: aapm
	---

	# APM
	"""
)
{
	[Fact]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Invalid products frontmatter value: \"aapm\". Did you mean \"apm\"?"));
	}
}

public class ProductsSuggestionWhenMispelled2(ITestOutputHelper output) : DirectiveTest(output,
	"""
	---
	products:
	  - id: apmagent
	---

	# APM
	"""
)
{
	[Fact]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Invalid products frontmatter value: \"apmagent\". Did you mean \"apm-agent\"?"));
	}
}

public class ProductsSuggestionWhenCasingError(ITestOutputHelper output) : DirectiveTest(output,
	"""
	---
	products:
	  - id: Apm
	---

	# APM
	"""
)
{
	[Fact]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Invalid products frontmatter value: \"Apm\". Did you mean \"apm\"?"));
	}
}

public class ProductsSuggestionWhenEmpty(ITestOutputHelper output) : DirectiveTest(output,
	"""
	---
	products:
	  - id: ""
	---

	# APM
	"""
)
{
	[Fact]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Invalid products frontmatter value: \"Product 'id' field is required."));
	}
}

public class MappedPagesValidUrl(ITestOutputHelper output) : DirectiveTest(output,
	"""
	---
	mapped_pages:
	  - "https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html"
	---

	# Test Page
	"""
)
{
	[Fact]
	public void NoErrors()
	{
		Collector.Diagnostics.Should().BeEmpty();
	}
}

public class MappedPagesInvalidUrl(ITestOutputHelper output) : DirectiveTest(output,
	"""
	---
	mapped_pages:
	  - "https://www.elastic.co/docs/get-started/deployment-options"
	---

	# Test Page
	"""
)
{
	[Fact]
	public void HasErrors()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Invalid mapped_pages URL: \"https://www.elastic.co/docs/get-started/deployment-options\". All mapped_pages URLs must start with \"https://www.elastic.co/guide\". Please update the URL to reference content under the Elastic documentation guide."));
	}
}

public class MappedPagesMixedUrls(ITestOutputHelper output) : DirectiveTest(output,
	"""
	---
	mapped_pages:
	  - "https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html"
	  - "https://www.elastic.co/docs/invalid-url"
	  - "https://www.elastic.co/guide/en/kibana/current/index.html"
	---

	# Test Page
	"""
)
{
	[Fact]
	public void HasErrorsForInvalidUrl()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Invalid mapped_pages URL: \"https://www.elastic.co/docs/invalid-url\". All mapped_pages URLs must start with \"https://www.elastic.co/guide\""));
	}
}

public class MappedPagesEmptyUrl(ITestOutputHelper output) : DirectiveTest(output,
	"""
	---
	mapped_pages:
	  - ""
	---

	# Test Page
	"""
)
{
	[Fact]
	public void NoErrorsForEmptyUrl()
	{
		// Empty URLs are ignored, no validation error should occur
		Collector.Diagnostics.Should().BeEmpty();
	}
}

public class MappedPagesExternalUrl(ITestOutputHelper output) : DirectiveTest(output,
	"""
	---
	mapped_pages:
	  - "https://github.com/elastic/docs-builder"
	---

	# Test Page
	"""
)
{
	[Fact]
	public void HasErrorsForExternalUrl()
	{
		Collector.Diagnostics.Should().HaveCount(1);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Invalid mapped_pages URL: \"https://github.com/elastic/docs-builder\". All mapped_pages URLs must start with \"https://www.elastic.co/guide\""));
	}
}
