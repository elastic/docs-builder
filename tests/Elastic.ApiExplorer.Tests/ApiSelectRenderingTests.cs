// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials;
using Elastic.ApiExplorer.Infrastructure;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiSelectRenderingTests
{
	[Test]
	public async Task Render_CustomDropdown_MatchesCopyPageMenu()
	{
		var html = await _ApiSelect.Create(new ApiSelectModel
		{
			AriaLabel = "Language",
			Id = "rail-one-lang",
			SyncGroup = "api-language",
			ExtraClass = "api-code-sample-lang",
			Options = [new ApiSelectOption("Console", "Console", true), new ApiSelectOption("Python", "Python", false)]
		}).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("class=\"api-select nav-select-dropdown api-code-sample-lang\"");
		html.Should().Contain("id=\"rail-one-lang\"");
		html.Should().Contain("data-sync-group=\"api-language\"");
		html.Should().Contain("api-select-trigger");
		html.Should().Contain("api-select-value");
		html.Should().Contain("api-page-actions-menu");
		html.Should().Contain("api-page-actions-option");
		html.Should().Contain("data-value=\"Console\"");
		html.Should().Contain("data-value=\"Python\"");
		html.Should().Contain("aria-selected=\"true\"");
		html.Should().Contain("#icon-chevron-down");
		html.Should().NotContain("<select");
		html.Should().NotContain("<option");
		html.Should().NotContain("class=\"nav-select\"");
	}
}
