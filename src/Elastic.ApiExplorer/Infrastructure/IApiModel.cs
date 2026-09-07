// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.ApiExplorer.Infrastructure;

public interface IApiModel : INavigationModel, IPageRenderer<ApiRenderContext>
{
	/// <summary>
	/// Readable CommonMark for this page, or <see langword="null"/> when the model has no page.
	/// </summary>
	Task<string?> RenderCommonMarkAsync(ApiRenderContext context, Cancel ctx = default) => Task.FromResult<string?>(null);
}

public interface IApiGroupingModel : IApiModel;
