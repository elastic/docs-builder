// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.Overlays.Flyout;

/// <summary>A panel that slides in from the edge over the page. It is a modal dialog, so focus is trapped and Escape closes it.</summary>
public sealed record FlyoutModel
{
	public required string Id { get; init; }
	public required string Title { get; init; }
	public required string OpenLabel { get; init; }
	public required IHtmlContent Body { get; init; }
}
