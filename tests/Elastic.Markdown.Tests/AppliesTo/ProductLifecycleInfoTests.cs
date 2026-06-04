// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.AppliesTo;

namespace Elastic.Markdown.Tests.AppliesTo;

public class ProductLifecycleInfoTests
{
	[Fact]
	public void Experimental_HasExpectedMetadata()
	{
		ProductLifecycleInfo.GetShortName(ProductLifecycle.Experimental).Should().Be("Experimental");
		ProductLifecycleInfo.GetDisplayText(ProductLifecycle.Experimental).Should().Be("Experimental");
		ProductLifecycleInfo.GetOrder(ProductLifecycle.Experimental).Should().Be(3);
	}

	[Fact]
	public void Experimental_IsLessMatureThanPreview()
	{
		ProductLifecycleInfo.GetOrder(ProductLifecycle.Experimental)
			.Should()
			.BeGreaterThan(ProductLifecycleInfo.GetOrder(ProductLifecycle.TechnicalPreview));
	}
}
