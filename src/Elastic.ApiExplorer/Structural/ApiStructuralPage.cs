// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;
using RazorSlices;

namespace Elastic.ApiExplorer.Structural;

public enum ApiStructuralKind
{
	Authentication,
	Servers
}

public record AuthenticationSchemeDisplay(
	string Id,
	string Heading,
	string Slug,
	HtmlString? DescriptionHtml,
	string? DescriptionMarkdown,
	string? CredentialExample
);

public record ApiServerDisplay(string Url, string? Description);

public record ApiStructuralPage(ApiStructuralKind Kind) : IApiModel
{
	public string Title => Kind == ApiStructuralKind.Authentication ? "Authentication" : "Servers";

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var viewModel = StructuralViewModel.Create(this, context);
		var slice = StructuralView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}

	public Task<string?> RenderCommonMarkAsync(ApiRenderContext context, Cancel ctx = default) =>
		Task.FromResult<string?>(StructuralCommonMark.Write(StructuralViewModel.Create(this, context)));
}

public class StructuralNavigationItem : ILeafNavigationItem<ApiStructuralPage>
{
	public StructuralNavigationItem(
		string? urlPathPrefix,
		string apiUrlSuffix,
		ApiStructuralPage page,
		IRootNavigationItem<IApiGroupingModel, INavigationItem> root,
		INodeNavigationItem<INavigationModel, INavigationItem> parent
	)
	{
		NavigationRoot = root;
		Model = page;
		NavigationTitle = page.Title;
		Parent = parent;
		Url = page.Kind == ApiStructuralKind.Authentication
			? ApiUrlBuilder.AuthenticationUrl(urlPathPrefix, apiUrlSuffix)
			: ApiUrlBuilder.ServersUrl(urlPathPrefix, apiUrlSuffix);
		Id = ShortId.Create(Url);
	}

	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
	public string Id { get; }
	public ApiStructuralPage Model { get; }
	public string Url { get; }
	public bool Hidden => false;
	public string NavigationTitle { get; }
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public int NavigationIndex { get; set; }

	public static IReadOnlyList<StructuralNavigationItem> Create(string? urlPathPrefix, string apiUrlSuffix, LandingNavigationItem root) =>
		[
			new(urlPathPrefix, apiUrlSuffix, new ApiStructuralPage(ApiStructuralKind.Authentication), root, root),
			new(urlPathPrefix, apiUrlSuffix, new ApiStructuralPage(ApiStructuralKind.Servers), root, root)
		];
}

public class StructuralViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiStructuralPage Page { get; init; }
	public required IReadOnlyList<AuthenticationSchemeDisplay> Schemes { get; init; }
	public required IReadOnlyList<ApiServerDisplay> Servers { get; init; }
	public required string EmptyMessage { get; init; }

	protected override string? LayoutPageTitle => Page.Title;

	protected override IReadOnlyList<ApiTocItem> GetTocItems() =>
		Page.Kind == ApiStructuralKind.Authentication ? Schemes.Select(scheme => new ApiTocItem(scheme.Heading, scheme.Slug)).ToArray() : [];

	public static StructuralViewModel Create(ApiStructuralPage page, ApiRenderContext context) =>
		page.Kind == ApiStructuralKind.Authentication
			? new StructuralViewModel(context)
			{
				Page = page,
				Schemes = ReadSchemes(context),
				Servers = [],
				EmptyMessage = "This API does not declare authentication schemes."
			}
			: new StructuralViewModel(context)
			{
				Page = page,
				Schemes = [],
				Servers = ReadServers(context.Model),
				EmptyMessage = "This API does not declare servers."
			};

	internal static IReadOnlyList<AuthenticationSchemeDisplay> ReadSchemes(ApiRenderContext context)
	{
		var schemes = context.Model.Components?.SecuritySchemes;
		if (schemes is not { Count: > 0 })
			return [];

		var displays = new List<AuthenticationSchemeDisplay>(schemes.Count);
		foreach (var (id, scheme) in schemes)
		{
			var resolved = Resolve(scheme, context.Model);
			var label = OpenApiAuthSchemeResolver.LabelFor(resolved) ?? id;
			var description = resolved?.Description;
			displays.Add(
				new AuthenticationSchemeDisplay(
					id,
					$"{label} ({id})",
					id.ToLowerInvariant(),
					string.IsNullOrEmpty(description) ? null : ApiMarkdown.Render(context, description),
					description,
					CredentialExample(resolved)
				)
			);
		}

		return displays;
	}

	internal static IReadOnlyList<ApiServerDisplay> ReadServers(OpenApiDocument document) =>
		(document.Servers ?? [])
			.Where(server => !string.IsNullOrEmpty(server.Url))
			.Select(server => new ApiServerDisplay(server.Url!, server.Description))
			.ToArray();

	private static IOpenApiSecurityScheme? Resolve(IOpenApiSecurityScheme scheme, OpenApiDocument document)
	{
		if (
			scheme is OpenApiSecuritySchemeReference { Reference.Id: { Length: > 0 } id }
			&& document.Components?.SecuritySchemes?.TryGetValue(id, out var listed) == true
		)
			return listed;
		return scheme;
	}

	private static string? CredentialExample(IOpenApiSecurityScheme? scheme) => scheme?.Type switch
	{
		SecuritySchemeType.ApiKey when scheme.In == ParameterLocation.Header && !string.IsNullOrEmpty(scheme.Name) =>
			$"{scheme.Name}: <value>",
		SecuritySchemeType.ApiKey when scheme.In == ParameterLocation.Query && !string.IsNullOrEmpty(scheme.Name) =>
			$"?{scheme.Name}=<value>",
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "basic", StringComparison.OrdinalIgnoreCase) =>
			"Authorization: Basic <credentials>",
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase) =>
			"Authorization: Bearer <token>",
		SecuritySchemeType.Http when !string.IsNullOrEmpty(scheme.Scheme) => $"Authorization: {scheme.Scheme} <token>",
		_ => null
	};
}

internal static class StructuralCommonMark
{
	public static string Write(StructuralViewModel page)
	{
		var markdown = new StringBuilder();
		var apiBaseUrl = page.CurrentNavigationItem.NavigationRoot.Url;
		ApiCommonMark.Heading(markdown, 1, page.Page.Title);
		if (page.Page.Kind == ApiStructuralKind.Authentication)
			WriteSchemes(markdown, page, apiBaseUrl);
		else
			WriteServers(markdown, page);
		return markdown.ToString();
	}

	private static void WriteSchemes(StringBuilder markdown, StructuralViewModel page, string apiBaseUrl)
	{
		if (page.Schemes.Count == 0)
		{
			ApiCommonMark.Paragraph(markdown, page.EmptyMessage);
			return;
		}

		foreach (var scheme in page.Schemes)
		{
			ApiCommonMark.Heading(markdown, 2, scheme.Heading);
			ApiCommonMark.Prepared(markdown, scheme.DescriptionMarkdown, apiBaseUrl);
			if (!string.IsNullOrEmpty(scheme.CredentialExample))
				ApiCommonMark.Fence(markdown, "http", scheme.CredentialExample);
		}
	}

	private static void WriteServers(StringBuilder markdown, StructuralViewModel page)
	{
		if (page.Servers.Count == 0)
		{
			ApiCommonMark.Paragraph(markdown, page.EmptyMessage);
			return;
		}

		foreach (var server in page.Servers)
		{
			var line = $"`{server.Url}`";
			if (!string.IsNullOrEmpty(server.Description))
				line += $" ({server.Description})";
			_ = markdown.AppendLine($"- {line}");
		}

		_ = markdown.AppendLine();
	}
}
