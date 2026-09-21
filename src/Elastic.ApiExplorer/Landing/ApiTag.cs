// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using RazorSlices;

namespace Elastic.ApiExplorer.Landing;

/// <summary>External documentation for an OpenAPI tag (tag-level <c>externalDocs</c>).</summary>
public record ApiTagExternalDoc(string? Description, string Url);

public record ApiTag(
	string Name,
	string DisplayName,
	string Description,
	ApiTagExternalDoc? ExternalDocs,
	string TagUrlSegment,
	IReadOnlyCollection<ApiEndpoint> Endpoints
) : IApiGroupingModel
{
	public object? CreatePageModel(ApiRenderContext context) => ApiOverviewBuilder.BuildTagChildren(context.CurrentNavigation);

	/// <inheritdoc />
	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, object? pageModel, Cancel ctx = default)
	{
		var overviewRows = pageModel as IReadOnlyList<ApiOverviewRow> ?? ApiOverviewBuilder.BuildTagChildren(context.CurrentNavigation);
		var viewModel = new TagLandingViewModel(context) { Tag = this, OverviewRows = overviewRows };
		var slice = TagLandingView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default) =>
		await RenderAsync(stream, context, null, ctx);

	public Task<string?> RenderCommonMarkAsync(ApiRenderContext context, object? pageModel, Cancel ctx = default)
	{
		var overviewRows = pageModel as IReadOnlyList<ApiOverviewRow> ?? ApiOverviewBuilder.BuildTagChildren(context.CurrentNavigation);
		var viewModel = new TagLandingViewModel(context) { Tag = this, OverviewRows = overviewRows };
		return Task.FromResult<string?>(LandingCommonMark.Tag(viewModel));
	}

	public Task<string?> RenderCommonMarkAsync(ApiRenderContext context, Cancel ctx = default) => RenderCommonMarkAsync(context, null, ctx);
}
