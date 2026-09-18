// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests;

public class RelatedLearningConfigurationTests
{
	[Fact]
	public void EmbeddedCatalog_LoadsWithCompleteEntries()
	{
		var config = LoadEmbeddedCatalog();

		config.Links.Should().NotBeEmpty();
		foreach (var (id, link) in config.Links)
		{
			id.Should().NotBeNullOrWhiteSpace();
			link.Id.Should().Be(id);
			link.Title.Should().NotBeNullOrWhiteSpace();
			Uri.TryCreate(link.Url, UriKind.Absolute, out var uri).Should().BeTrue($"link '{id}' url '{link.Url}' should be absolute");
			uri!.Scheme.Should().BeOneOf(Uri.UriSchemeHttps, Uri.UriSchemeHttp);
		}
	}

	[Fact]
	public void Parse_QuotedTitleWithColonAndAmpersand_Succeeds()
	{
		var config = RelatedLearningConfiguration.Parse(
			"""
			links:
			  sample:
			    title: "Beyond basics: Hugging Face & Elasticsearch"
			    url: https://www.elastic.co/blog/sample
			"""
		);

		config.TryGet("sample", out var link).Should().BeTrue();
		link!.Title.Should().Be("Beyond basics: Hugging Face & Elasticsearch");
	}

	[Fact]
	public void TryGet_UnknownId_ReturnsFalse()
	{
		var config = LoadEmbeddedCatalog();

		config.TryGet("not-a-module", out var link).Should().BeFalse();
		link.Should().BeNull();
	}

	[Fact]
	public void Parse_MissingTitle_Throws()
	{
		var act =
			() => RelatedLearningConfiguration.Parse(
				"""
			links:
			  widget:
			    url: https://www.elastic.co/training/widget
			"""
			);

		act.Should().Throw<InvalidOperationException>().WithMessage("*link 'widget' is missing required 'title'*");
	}

	[Fact]
	public void Parse_MissingUrl_Throws()
	{
		var act =
			() => RelatedLearningConfiguration.Parse("""
			links:
			  widget:
			    title: Widget
			""");

		act.Should().Throw<InvalidOperationException>().WithMessage("*link 'widget' is missing required 'url'*");
	}

	[Fact]
	public void Parse_RelativeUrl_Throws()
	{
		var act =
			() => RelatedLearningConfiguration.Parse(
				"""
			links:
			  widget:
			    title: Widget
			    url: /training/widget
			"""
			);

		act.Should().Throw<InvalidOperationException>().WithMessage("*invalid url '/training/widget'*");
	}

	[Fact]
	public void Parse_EmptyLinks_ReturnsEmptyCatalog()
	{
		var config = RelatedLearningConfiguration.Parse("links: {}");

		config.Links.Should().BeEmpty();
	}

	private static RelatedLearningConfiguration LoadEmbeddedCatalog()
	{
		var provider = new ConfigurationFileProvider(new NullLoggerFactory(), new ConfigurationFileSystem());
		return provider.CreateRelatedLearningConfiguration();
	}
}
