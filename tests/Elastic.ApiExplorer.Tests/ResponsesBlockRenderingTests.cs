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
using Nullean.Snapshots.Html;
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

		html.Should().MatchHtmlSnapshot();
		html.Should().NotContain("<select", "the statuses are clickable pills, not a dropdown");
		html.Should().NotContain("id=\"response-200-fields\" hidden", "the body has no hidden attribute, so it shows on load");
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
			Response = new OpenApiResponse { Description = "Invalid input data response" },
			Contents =
			[
				new ApiMediaContent
				{
					ContentType = "application/json",
					Type = new TypeAnnotation([new TypeSpan("union oneOf")]),
					Properties = null,
					UnionVariants = new ApiUnionVariants
					{
						Variants =
						[
							Variant("PlatformErrorResponse", "res-400-variant-platform"),
							Variant("SiemErrorResponse", "res-400-variant-siem")
						],
						ContainerId = "res-400-union-options"
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
			Response = new OpenApiResponse { Description = "Invalid input data response" },
			Contents =
			[
				new ApiMediaContent
				{
					ContentType = "application/json",
					Type = new TypeAnnotation([new TypeSpan("union oneOf")]),
					Properties = null,
					UnionVariants = new ApiUnionVariants
					{
						Variants = [Variant(displayName, "res-400-variant-x")],
						ContainerId = "res-400-union-options"
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
	public async Task Render_Response_PutsStatusDescriptionAndMediaTypeInThreeColumns()
	{
		var html = await RenderHtml(
			Response("200", "success", "A JSON object containing pipelines statistics.\n\n- queue depth\n- worker utilization")
		);

		var button = html[html.IndexOf("<button", StringComparison.Ordinal)..html.IndexOf("</button>", StringComparison.Ordinal)];
		button.Should().Contain("response-status-chip");
		button.Should().NotContain("application/json");
		button.Should().NotContain("queue depth");
		button.Should().NotContain("<ul>");
		html.Should().Contain("<li>queue depth</li>");
		var status = html.IndexOf("</button>", StringComparison.Ordinal);
		var description = html.IndexOf("response-description", StringComparison.Ordinal);
		var mediaType = html.IndexOf("content-type-tag", StringComparison.Ordinal);
		var icon = html.IndexOf("response-toggle-icon", StringComparison.Ordinal);
		description.Should().BeGreaterThan(status);
		mediaType.Should().BeGreaterThan(description);
		icon.Should().BeGreaterThan(mediaType);
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
		var html = await RenderHtml(Response("200", "success", "Indicates a successful response") with
		{
			Contents = [new ApiMediaContent { ContentType = "application/json", Type = null, Properties = null }]
		});

		html.Should().MatchHtmlSnapshot();
		html.Should().NotContain("response-panel-body", "a response with nothing to show has no body to expand");
	}

	[Test]
	public async Task Render_ResponseWithTwoContentTypes_TagsTheActiveTypeAndPicksFromASelectInTheBody()
	{
		var response = Response("200", "success", "OK", "application/json");
		var binary = new ApiMediaContent
		{
			ContentType = "application/zip",
			Type = new TypeAnnotation([new TypeSpan("string(binary)")]),
			Properties = null
		};

		var html = await RenderHtml(response with { Contents = [.. response.Contents, binary] });

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_ResponseWithOneContentType_TagsItAndHasNoPicker()
	{
		var html = await RenderHtml(Response("200", "success", "OK"));

		html.Should().Contain("<span class=\"content-type-tag\">application/json</span>");
		html.Should().NotContain("data-content-type-current");
		html.Should().NotContain("data-content-type-select");
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

		return new HtmlString(ApiHtmlSanitizer.Sanitize(Markdig.Markdown.ToHtml(markdown, MarkdownParser.ApiDescriptionPipeline)));
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
			Response = new OpenApiResponse { Description = description },
			Contents =
			[
				new ApiMediaContent { ContentType = contentType, Type = new TypeAnnotation([new TypeSpan("object")]), Properties = null }
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
