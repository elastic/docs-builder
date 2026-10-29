// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Topics;
using Elastic.Documentation;
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

public record ApiServerDisplay(string Url, string? Description);

/// <param name="Intro">Markdown shown above the schemes, from an <c>x-topics</c> entry titled Authentication.</param>
public record ApiStructuralPage(ApiStructuralKind Kind, string? Intro = null) : IApiModel<StructuralViewModel>
{
	public string Title => Kind == ApiStructuralKind.Authentication ? "Authentication" : "Servers";

	public StructuralViewModel? CreatePageModel(ApiRenderContext context) => StructuralViewModel.Create(this, context);

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, StructuralViewModel? pageModel, Cancel ctx = default)
	{
		var viewModel = pageModel ?? StructuralViewModel.Create(this, context);
		var slice = StructuralView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}

	public Task<string?> RenderCommonMarkAsync(ApiRenderContext context, StructuralViewModel? pageModel, Cancel ctx = default)
	{
		var viewModel = pageModel ?? StructuralViewModel.Create(this, context);
		return Task.FromResult<string?>(StructuralCommonMark.Write(viewModel));
	}
}

public class StructuralNavigationItem : ApiPageNavigationItem<ApiStructuralPage>
{
	public StructuralNavigationItem(
		string? urlPathPrefix,
		string apiUrlSuffix,
		ApiStructuralPage page,
		IRootNavigationItem<IApiGroupingModel, INavigationItem> root,
		INodeNavigationItem<INavigationModel, INavigationItem> parent
	) : base(UrlFor(urlPathPrefix, apiUrlSuffix, page.Kind), page.Title, page, root, parent) =>
		// Authentication and Servers are still generated so old Bump.sh links keep working; operation pages show both inline.
		// An Authentication page written as a topic is prose a reader looks for, so it is listed with the other topics.
		Hidden = true;

	private static string UrlFor(string? urlPathPrefix, string apiUrlSuffix, ApiStructuralKind kind) =>
		kind == ApiStructuralKind.Authentication
			? ApiUrlBuilder.AuthenticationUrl(urlPathPrefix, apiUrlSuffix)
			: ApiUrlBuilder.ServersUrl(urlPathPrefix, apiUrlSuffix);

	public static IReadOnlyList<StructuralNavigationItem> Create(
		string? urlPathPrefix,
		string apiUrlSuffix,
		LandingNavigationItem root,
		OpenApiDocument document
	)
	{
		var items = new List<StructuralNavigationItem>();
		if (StructuralViewModel.HasSchemes(document))
			items.Add(new(urlPathPrefix, apiUrlSuffix, new ApiStructuralPage(ApiStructuralKind.Authentication), root, root));
		if (StructuralViewModel.ReadServers(document).Count > 0)
			items.Add(new(urlPathPrefix, apiUrlSuffix, new ApiStructuralPage(ApiStructuralKind.Servers), root, root));
		return items;
	}

	/// <summary>The Authentication page the Topics heading lists when the spec declares schemes, with no prose of its own.</summary>
	public static StructuralNavigationItem ForGeneratedAuthentication(
		string? urlPathPrefix,
		string apiUrlSuffix,
		LandingNavigationItem root
	) => new(urlPathPrefix, apiUrlSuffix, new ApiStructuralPage(ApiStructuralKind.Authentication), root, root) { Hidden = false };

	/// <summary>
	/// The Authentication page for a spec that writes it as an <c>x-topics</c> entry, with or without security schemes.
	/// The caller sets <see cref="Parent"/> to the Topics heading.
	/// </summary>
	public static StructuralNavigationItem ForAuthenticationTopic(
		string? urlPathPrefix,
		string apiUrlSuffix,
		LandingNavigationItem root,
		ApiTopicPage topic
	)
	{
		var page = new ApiStructuralPage(ApiStructuralKind.Authentication, topic.Content);
		return new StructuralNavigationItem(urlPathPrefix, apiUrlSuffix, page, root, root) { Hidden = false };
	}
}

public class StructuralViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	/// <summary>The id of the Authentication section, which an intro heading with the same slug must not take.</summary>
	private const string AuthenticationAnchor = "authentication";

	public required ApiStructuralPage Page { get; init; }
	public required IReadOnlyList<AuthRequirement> Requirements { get; init; }
	public required IReadOnlyList<ApiServerDisplay> Servers { get; init; }
	public required string EmptyMessage { get; init; }
	public bool AuthenticationOptional { get; init; }
	public HtmlString? IntroHtml { get; init; }
	public IReadOnlyList<TocItem> IntroHeadings { get; init; } = [];

	/// <summary>The empty message only shows when the page has nothing else to say.</summary>
	public bool ShowEmptyMessage =>
		IntroHtml is null && (Page.Kind == ApiStructuralKind.Authentication ? Requirements.Count == 0 : Servers.Count == 0);

	public AuthenticationSectionModel Authentication => new(Requirements, AuthenticationOptional, markdown => RenderMarkdown(markdown));

	protected override string? LayoutPageTitle => Page.Title;

	protected override IReadOnlyList<TocItem> GetTocItems() =>
		Page.Kind == ApiStructuralKind.Authentication
			? [
				.. IntroHeadings,
				.. Requirements.Count > 0 ? [new TocItem("Authentication", AuthenticationAnchor, 2)] : Array.Empty<TocItem>()
			]
			: [];

	public static StructuralViewModel Create(ApiStructuralPage page, ApiRenderContext context)
	{
		if (page.Kind != ApiStructuralKind.Authentication)
		{
			return new StructuralViewModel(context)
			{
				Page = page,
				Requirements = [],
				Servers = ReadServers(context.Model),
				EmptyMessage = "This API does not declare servers."
			};
		}

		var (introHtml, introHeadings) = RenderIntro(page.Intro, context);
		return new StructuralViewModel(context)
		{
			Page = page,
			Requirements = ReadRequirements(context.Model),
			AuthenticationOptional = OpenApiAuthSchemeResolver.AllowsAnonymous(context.Model.Security),
			Servers = [],
			EmptyMessage = "This API does not declare authentication schemes.",
			IntroHtml = introHtml,
			IntroHeadings = introHeadings
		};
	}

	// The Authentication section owns its anchor, so an intro heading with the same slug gets none.
	private static (HtmlString? Html, IReadOnlyList<TocItem> Headings) RenderIntro(string? intro, ApiRenderContext context)
	{
		if (string.IsNullOrWhiteSpace(intro))
			return (null, []);

		return ApiMarkdown.RenderWithHeadings(context, intro, new HashSet<string>([AuthenticationAnchor], StringComparer.Ordinal));
	}

	internal static bool HasSchemes(OpenApiDocument document) => document.Components?.SecuritySchemes is { Count: > 0 };

	internal static IReadOnlyList<AuthRequirement> ReadRequirements(OpenApiDocument document) =>
		OpenApiAuthSchemeResolver.ResolveDocument(document);

	internal static IReadOnlyList<ApiServerDisplay> ReadServers(OpenApiDocument document) => ToDisplays(document.Servers);

	internal static IReadOnlyList<ApiServerDisplay> ToDisplays(IEnumerable<OpenApiServer>? servers) =>
		(servers ?? [])
			.Where(server => !string.IsNullOrEmpty(server.Url))
			.Select(server => new ApiServerDisplay(server.Url!, server.Description))
			.ToArray();
}

internal static class StructuralCommonMark
{
	public static string Write(StructuralViewModel page)
	{
		var markdown = new StringBuilder();
		var apiBaseUrl = page.CurrentNavigationItem.NavigationRoot.Url;
		ApiCommonMark.Heading(markdown, 1, page.Page.Title);
		ApiCommonMark.Prepared(markdown, page.Page.Intro, apiBaseUrl);
		if (page.Page.Kind == ApiStructuralKind.Authentication)
			WriteSchemes(markdown, page, apiBaseUrl);
		else
			WriteServers(markdown, page);
		return markdown.ToString();
	}

	private static void WriteSchemes(StringBuilder markdown, StructuralViewModel page, string apiBaseUrl)
	{
		if (page.ShowEmptyMessage)
		{
			ApiCommonMark.Paragraph(markdown, page.EmptyMessage);
			return;
		}

		if (page.AuthenticationOptional)
			ApiCommonMark.Paragraph(markdown, "Authentication is optional.");
		foreach (var requirement in page.Requirements)
			OperationCommonMark.WriteRequirement(markdown, requirement, apiBaseUrl);
		if (page.Requirements.Count > 0)
			_ = markdown.AppendLine();
	}

	private static void WriteServers(StringBuilder markdown, StructuralViewModel page)
	{
		if (page.ShowEmptyMessage)
		{
			ApiCommonMark.Paragraph(markdown, page.EmptyMessage);
			return;
		}

		foreach (var server in page.Servers)
		{
			var line = ApiCommonMark.CodeSpan(server.Url);
			if (!string.IsNullOrEmpty(server.Description))
				line += $" ({server.Description})";
			_ = markdown.AppendLine($"- {line}");
		}

		_ = markdown.AppendLine();
	}
}
