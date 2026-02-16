// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site;
using FluentAssertions;
using Xunit;

namespace Elastic.Markdown.Tests.Codex;

/// <summary>Tests for Codex HTMX attribute provider behavior.</summary>
public class CodexHtmxAttributeProviderTests
{
	[Fact]
	public void GetRootPath_ReturnsConfiguredRootPath()
	{
		var provider = new CodexHtmxAttributeProvider("/internal-docs/");
		provider.GetRootPath().Should().Be("/internal-docs/");
	}

	[Fact]
	public void GetHxSelectOob_SameTopLevelGroup_ReturnsContentContainerAndTocNav()
	{
		var provider = new CodexHtmxAttributeProvider("/");
		provider.GetHxSelectOob(hasSameTopLevelGroup: true)
			.Should().Be("#content-container,#toc-nav");
	}

	[Fact]
	public void GetHxSelectOob_DifferentTopLevelGroup_ReturnsMainContainer()
	{
		var provider = new CodexHtmxAttributeProvider("/");
		provider.GetHxSelectOob(hasSameTopLevelGroup: false)
			.Should().Be("#main-container");
	}

	[Fact]
	public void GetHxAttributes_IncludesPreload()
	{
		var provider = new CodexHtmxAttributeProvider("/");
		var attrs = provider.GetHxAttributes(hasSameTopLevelGroup: false);
		attrs.Should().Contain("preload=");
	}

	[Fact]
	public void GetNavHxAttributes_SameGroup_ReturnsContentContainerAndTocNav()
	{
		var provider = new CodexHtmxAttributeProvider("/");
		var attrs = provider.GetNavHxAttributes(hasSameTopLevelGroup: true);
		attrs.Should().Contain("hx-select-oob=#content-container,#toc-nav");
	}

	[Fact]
	public void GetNavHxAttributes_DifferentGroup_ReturnsMainContainer()
	{
		var provider = new CodexHtmxAttributeProvider("/");
		var attrs = provider.GetNavHxAttributes(hasSameTopLevelGroup: false);
		attrs.Should().Contain("hx-select-oob=#main-container");
	}
}
