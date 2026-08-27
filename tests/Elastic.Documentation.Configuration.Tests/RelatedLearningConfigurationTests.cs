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
	public void EmbeddedCatalog_LoadsStarterTrainingIds()
	{
		var config = LoadEmbeddedCatalog();

		config.TryGet("apm-with-elastic", out var apm).Should().BeTrue();
		apm!.Title.Should().Be("APM with Elastic");
		apm.Url.Should().Be("https://www.elastic.co/training/apm-with-elastic");

		config.TryGet("elastic-agent", out _).Should().BeTrue();
		config.TryGet("index-basics", out _).Should().BeTrue();
		config.TryGet("data-types-and-mappings", out _).Should().BeTrue();
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
		var act = () => RelatedLearningConfiguration.Parse(
			"""
			links:
			  widget:
			    url: https://www.elastic.co/training/widget
			""");

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*link 'widget' is missing required 'title'*");
	}

	[Fact]
	public void Parse_MissingUrl_Throws()
	{
		var act = () => RelatedLearningConfiguration.Parse(
			"""
			links:
			  widget:
			    title: Widget
			""");

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*link 'widget' is missing required 'url'*");
	}

	[Fact]
	public void Parse_RelativeUrl_Throws()
	{
		var act = () => RelatedLearningConfiguration.Parse(
			"""
			links:
			  widget:
			    title: Widget
			    url: /training/widget
			""");

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*invalid url '/training/widget'*");
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
