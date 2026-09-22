// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.ApiExplorer.Infrastructure;

public interface IApiModel : INavigationModel, IPageRenderer<ApiRenderContext>
{
	/// <summary>
	/// Builds the expensive page model once; the result is passed to both
	/// <see cref="RenderAsync(FileSystemStream, ApiRenderContext, object?, Cancel)"/> and
	/// <see cref="RenderCommonMarkAsync(ApiRenderContext, object?, Cancel)"/> so it is not
	/// computed twice per rendered page.
	/// </summary>
	object? CreatePageModel(ApiRenderContext context) => null;

	/// <summary>
	/// Renders the HTML page using a pre-built <paramref name="pageModel"/> produced by
	/// <see cref="CreatePageModel"/>. Defaults to the no-model overload so implementations
	/// that do not override <see cref="CreatePageModel"/> still work.
	/// </summary>
	Task RenderAsync(FileSystemStream stream, ApiRenderContext context, object? pageModel, Cancel ctx = default) =>
		// Cast to the base interface to avoid C# overload resolution boxing `ctx` (CancellationToken)
		// to object? and re-entering this same default method infinitely.
		((IPageRenderer<ApiRenderContext>)this).RenderAsync(stream, context, ctx);

	/// <summary>
	/// Readable CommonMark for this page, or <see langword="null"/> when the model has no page.
	/// </summary>
	Task<string?> RenderCommonMarkAsync(ApiRenderContext context, Cancel ctx = default) => Task.FromResult<string?>(null);

	/// <summary>
	/// Renders CommonMark using a pre-built <paramref name="pageModel"/> produced by
	/// <see cref="CreatePageModel"/>. Returns <see langword="null"/> by default; implementations
	/// that override <see cref="RenderCommonMarkAsync(ApiRenderContext, Cancel)"/> must also
	/// override this overload to avoid silently losing their output.
	/// </summary>
	// Do NOT delegate to RenderCommonMarkAsync(context, ctx) here. C# overload resolution inside
	// a default interface method boxes `ctx` (CancellationToken) to object? and re-enters this
	// same method — infinite recursion. Return null directly and require explicit overrides.
	Task<string?> RenderCommonMarkAsync(ApiRenderContext context, object? pageModel, Cancel ctx = default) =>
		Task.FromResult<string?>(null);
}

public interface IApiGroupingModel : IApiModel;
