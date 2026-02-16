// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site;
using FluentAssertions;
using Xunit;

namespace Elastic.Markdown.Tests.Codex;

/// <summary>Tests for default HTMX attribute provider (isolated/assembler) behavior.</summary>
public class DefaultHtmxAttributeProviderTests
{
	[Fact]
	public void GetRootPath_ReturnsConfiguredRootPath()
	{
		var provider = new DefaultHtmxAttributeProvider("/docs/");
		provider.GetRootPath().Should().Be("/docs/");
	}

	[Fact]
	public void GetHxSelectOob_SameTopLevelGroup_ReturnsContentContainerAndTocNav()
	{
		var provider = new DefaultHtmxAttributeProvider("/");
		provider.GetHxSelectOob(hasSameTopLevelGroup: true)
			.Should().Be("#content-container,#toc-nav");
	}

	[Fact]
	public void GetHxSelectOob_DifferentTopLevelGroup_ReturnsGranularSwap()
	{
		var provider = new DefaultHtmxAttributeProvider("/");
		provider.GetHxSelectOob(hasSameTopLevelGroup: false)
			.Should().Be("#content-container,#toc-nav,#nav-tree,#nav-dropdown");
	}

	[Fact]
	public void CodexVsDefault_DifferentGroup_ReturnsDifferentOob()
	{
		var codex = new CodexHtmxAttributeProvider("/");
		var defaultProvider = new DefaultHtmxAttributeProvider("/");

		codex.GetHxSelectOob(hasSameTopLevelGroup: false).Should().Be("#main-container");
		defaultProvider.GetHxSelectOob(hasSameTopLevelGroup: false)
			.Should().Be("#content-container,#toc-nav,#nav-tree,#nav-dropdown");
	}
}
