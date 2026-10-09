// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.ButtonGroup;

public sealed record ButtonGroupModel
{
	/// <summary>The rendered buttons of the group.</summary>
	public required IHtmlContent Buttons { get; init; }

	public string? Align { get; init; }

	public string CssClass => string.IsNullOrEmpty(Align) ? "button-group" : $"button-group button-group--{Align}";
}
