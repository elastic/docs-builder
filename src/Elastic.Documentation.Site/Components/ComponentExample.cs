// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using RazorSlices;

namespace Elastic.Documentation.Site.Components;

/// <summary>
/// One canonical use of a component. The style guide renders it live and shows its HTML, and tests assert against it,
/// so there is a single definition of what each component looks like.
/// </summary>
public sealed class ComponentExample
{
	private readonly Func<RazorSlice> _create;

	private ComponentExample(string title, string modelCode, Func<RazorSlice> create)
	{
		Title = title;
		ModelCode = modelCode;
		_create = create;
	}

	public string Title { get; }

	/// <summary>The model expression exactly as written in the example, for display in the style guide.</summary>
	public string ModelCode { get; }

	/// <summary>Set for examples shown on a dark or otherwise pinned surface.</summary>
	public string? Theme { get; init; }

	public static ComponentExample Of<TModel>(
		string title,
		Func<TModel, RazorSlice<TModel>> create,
		TModel model,
		[CallerArgumentExpression(nameof(model))] string modelCode = ""
	) => new(title, modelCode, () => create(model));

	public async Task<string> RenderAsync(Cancel ctx = default)
	{
		using var writer = new StringWriter();
		await _create().RenderAsync(writer, cancellationToken: ctx);
		return writer.ToString().Trim();
	}
}

public sealed record ComponentSection(string Title, string Intro, string ClassName, IReadOnlyList<ComponentExample> Examples);
