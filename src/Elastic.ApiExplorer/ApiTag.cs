// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Landing;
using RazorSlices;

namespace Elastic.ApiExplorer;

/// <summary>External documentation for an OpenAPI tag (tag-level <c>externalDocs</c>).</summary>
public record ApiTagExternalDoc(string? Description, string Url);

public record ApiTag(
	string Name,
	string DisplayName,
	string Description,
	ApiTagExternalDoc? ExternalDocs,
	string TagUrlSegment,
	IReadOnlyCollection<ApiEndpoint> Endpoints) : IApiGroupingModel
{
	/// <inheritdoc />
	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var viewModel = new TagLandingViewModel(context)
		{
			Tag = this,
			OverviewRows = ApiOverviewBuilder.BuildTagChildren(context.CurrentNavigation)
		};
		var slice = TagLandingView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}
}
