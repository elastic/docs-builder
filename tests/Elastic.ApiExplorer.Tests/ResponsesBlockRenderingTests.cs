// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Operations._Partials;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ResponsesBlockRenderingTests
{
	[Fact]
	public async Task Render_MultipleStatuses_UsesClickablePillsInsteadOfSelect()
	{
		var html = await RenderHtml(
			Response("200", "success", "Successful response"),
			Response("400", "error", "Bad request", "text/plain")
		);

		html.Should().Contain("Responses");
		html.Should().Contain("response-status-toggle");
		html.Should().Contain("response-status-chip status-success");
		html.Should().Contain("response-status-chip status-error");
		html.Should().Contain("Successful response");
		html.Should().Contain("Bad request");
		html.Should().Contain("application/json");
		html.Should().NotContain("text/plain");
		html.Should().Contain("aria-controls=\"response-200-fields\"");
		html.Should().Contain("aria-controls=\"response-400-fields\"");
		html.Should().Contain("aria-expanded=\"false\"");
		html.Should().Contain("response-panel collapsed");
		html.Should().Contain("id=\"response-200-fields\"");
		html.Should().Contain("id=\"response-400-fields\"");
		html.Should().NotContain("<select");
		html.Should().NotContain("<option");
		html.Should().NotContain("api-responses-select");
		html.Should().NotContain("show fields");
		html.Should().NotContain("response-fields-toggle");
		html.Should().NotContain("hidden=\"hidden\"");
	}

	[Fact]
	public async Task Render_SingleStatus_KeepsSingularHeading()
	{
		var html = await RenderHtml(Response("200", "success", "Successful response"));

		html.Should().Contain(">Response</span>");
		html.Should().NotContain(">Responses</span>");
		html.Should().Contain("aria-controls=\"response-200-fields\"");
	}

	[Fact]
	public async Task Render_OneOfResponse_RendersUnionVariantsInsteadOfTypeLine()
	{
		var html = await RenderHtml(new ApiResponse
		{
			StatusCode = "400",
			StatusClass = "error",
			FirstContentType = "application/json",
			Response = new OpenApiResponse { Description = "Invalid input data response" },
			Contents =
			[
				new ApiResponseContent
				{
					ContentType = "application/json",
					Type = new TypeAnnotation([new TypeSpan("union oneOf")]),
					Properties = null,
					ArrayItemProperties = null,
					UnionVariants = new ApiUnionVariants
					{
						Variants =
						[
							Variant("PlatformErrorResponse", "res-400-variant-platform"),
							Variant("SiemErrorResponse", "res-400-variant-siem")
						],
						ShouldCollapse = false,
						ContainerId = "res-400-union-options",
						UseHiddenUntilFound = false
					}
				}
			],
			Headers = []
		});

		html.Should().Contain("PlatformErrorResponse");
		html.Should().Contain("SiemErrorResponse");
		html.Should().Contain("union-variant-item");
		html.Should().NotContain("Response Type:");
	}

	private static async Task<string> RenderHtml(params ApiResponse[] responses)
	{
		var model = new ResponsesBlockModel(responses, markdown => new HtmlString(markdown ?? ""));
		return await _ResponsesBlock.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
	}

	private static ApiResponse Response(
		string statusCode,
		string statusClass,
		string description,
		string contentType = "application/json"
	) =>
		new()
		{
			StatusCode = statusCode,
			StatusClass = statusClass,
			FirstContentType = contentType,
			Response = new OpenApiResponse { Description = description },
			Contents = [],
			Headers = []
		};

	private static ApiUnionVariant Variant(string displayName, string anchorId) =>
		new()
		{
			DisplayName = displayName,
			IsArrayVariant = false,
			IsObjectType = true,
			AnchorId = anchorId,
			ShowProperties = false,
			IsCollapsible = false,
			DefaultExpanded = false,
			NestedCount = 0,
			UseHidden = false
		};
}
