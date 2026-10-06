// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Operations._Partials;
using Elastic.Markdown.Myst;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ResponsesBlockRenderingTests
{
	[Test]
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
		html.Should().Contain("text/plain");
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

	[Test]
	public async Task Render_SingleStatus_KeepsSingularHeading()
	{
		var html = await RenderHtml(Response("200", "success", "Successful response"));

		html.Should().Contain(">Response</a>");
		html.Should().NotContain(">Responses</a>");
		html.Should().Contain("aria-controls=\"response-200-fields\"");
	}

	[Test]
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

	[Test]
	[Arguments("bedrock_config")]
	[Arguments("Security_Lists_API_list_item")]
	[Arguments("PlatformErrorResponse")]
	public async Task Render_UnionVariant_ShowsItsNameWhateverTheIdStyle(string displayName)
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
						Variants = [Variant(displayName, "res-400-variant-x")],
						ShouldCollapse = false,
						ContainerId = "res-400-union-options",
						UseHiddenUntilFound = false
					}
				}
			],
			Headers = []
		});

		html.Should().Contain($">{displayName}</span></code>");
	}

	[Test]
	public async Task Render_ResponseDescription_RendersInlineCode()
	{
		var html = await RenderHtml(Response("200", "success", "Disabled by the `alerting:v2:enabled` setting."));

		html.Should().Contain("<code>alerting:v2:enabled</code>");
		html.Should().NotContain("`alerting:v2:enabled`");
	}

	[Test]
	public async Task Render_Response_PutsStatusAndMediaTypeAboveTheDescription()
	{
		var html = await RenderHtml(
			Response("200", "success", "A JSON object containing pipelines statistics.\n\n- queue depth\n- worker utilization")
		);

		var button = html[html.IndexOf("<button", StringComparison.Ordinal)..html.IndexOf("</button>", StringComparison.Ordinal)];
		button.Should().Contain("response-status-chip");
		button.Should().Contain("application/json");
		button.Should().Contain("response-toggle-icon");
		button.Should().NotContain("queue depth");
		button.Should().NotContain("<ul>");
		html.Should().Contain("<ul>");
		html.Should().Contain("<li>queue depth</li>");
		html
			.IndexOf("response-description", StringComparison.Ordinal)
			.Should()
			.BeGreaterThan(html.IndexOf("</button>", StringComparison.Ordinal));
		html
			.IndexOf("content-type-tag", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("response-description", StringComparison.Ordinal));
	}

	[Test]
	public async Task Render_ResponseDescriptionWithLink_PlacesAnchorOutsideTheToggle()
	{
		var html = await RenderHtml(Response("200", "success", "See the [guide](https://www.elastic.co/guide)."));

		var button = html[html.IndexOf("<button", StringComparison.Ordinal)..html.IndexOf("</button>", StringComparison.Ordinal)];
		button.Should().NotContain("<a ");
		html.Should().Contain("href=\"https://www.elastic.co/guide\"");
		html
			.IndexOf("href=\"https://www.elastic.co/guide\"", StringComparison.Ordinal)
			.Should()
			.BeGreaterThan(html.IndexOf("</button>", StringComparison.Ordinal));
	}

	[Test]
	public async Task Render_ResponseWithoutSchemaOrHeaders_RendersStaticRowWithoutEmptyBody()
	{
		var html = await RenderHtml(Response("200", "success", "Indicates a successful response") with { Contents = [] });

		html.Should().Contain("response-panel--static");
		html.Should().Contain("response-status-row");
		html.Should().Contain("Indicates a successful response");
		html.Should().Contain("application/json");
		html.Should().NotContain("response-status-toggle");
		html.Should().NotContain("response-toggle-icon");
		html.Should().NotContain("response-panel-body");
		html.Should().NotContain("response-200-fields");
	}

	private static async Task<string> RenderHtml(params ApiResponse[] responses)
	{
		var model = new ResponsesBlockModel(responses, RenderDescription);
		return await _ResponsesBlock.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
	}

	private static HtmlString RenderDescription(string? markdown)
	{
		if (string.IsNullOrEmpty(markdown))
			return HtmlString.Empty;

		return new HtmlString(ApiMarkdown.SanitizeHtml(Markdig.Markdown.ToHtml(markdown, MarkdownParser.ApiDescriptionPipeline)));
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
			Contents =
			[
				new ApiResponseContent
				{
					ContentType = contentType,
					Type = new TypeAnnotation([new TypeSpan("object")]),
					Properties = null,
					ArrayItemProperties = null
				}
			],
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
