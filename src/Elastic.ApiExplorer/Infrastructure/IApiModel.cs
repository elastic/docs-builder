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

/// <summary>
/// Typed variant of <see cref="IApiModel"/> for implementations whose page model is a concrete
/// reference type.  Provides compile-time–safe render methods and wires the non-generic
/// <see cref="IApiModel"/> surface via default interface method bridges so implementing classes
/// need only provide the three typed members.
/// </summary>
public interface IApiModel<TPageModel> : IApiModel where TPageModel : class
{
	/// <summary>Builds the page model; typed return replaces the <see cref="IApiModel.CreatePageModel"/> <see langword="object?"/>.</summary>
	new TPageModel? CreatePageModel(ApiRenderContext context);

	/// <summary>Renders HTML using the pre-built <paramref name="pageModel"/>; called with <see langword="null"/> when none was supplied.</summary>
	Task RenderAsync(FileSystemStream stream, ApiRenderContext context, TPageModel? pageModel, Cancel ctx = default);

	/// <summary>Renders CommonMark using the pre-built <paramref name="pageModel"/>; returns <see langword="null"/> by default.</summary>
	Task<string?> RenderCommonMarkAsync(ApiRenderContext context, TPageModel? pageModel, Cancel ctx = default) =>
		Task.FromResult<string?>(null);

	// ── IApiModel bridges ──────────────────────────────────────────────────────────────────────

	// Route the non-generic object? methods through the typed overloads so the OpenApiGenerator
	// call site (which knows only IApiModel) transparently benefits from the typed implementation.

	object? IApiModel.CreatePageModel(ApiRenderContext context) => CreatePageModel(context);

	Task IApiModel.RenderAsync(FileSystemStream stream, ApiRenderContext context, object? pageModel, Cancel ctx) =>
		RenderAsync(stream, context, pageModel as TPageModel, ctx);

	Task<string?> IApiModel.RenderCommonMarkAsync(ApiRenderContext context, Cancel ctx) => RenderCommonMarkAsync(context, default, ctx);

	Task<string?> IApiModel.RenderCommonMarkAsync(ApiRenderContext context, object? pageModel, Cancel ctx) =>
		RenderCommonMarkAsync(context, pageModel as TPageModel, ctx);

	Task IPageRenderer<ApiRenderContext>.RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx) =>
		RenderAsync(stream, context, default, ctx);
}

public interface IApiGroupingModel : IApiModel;
