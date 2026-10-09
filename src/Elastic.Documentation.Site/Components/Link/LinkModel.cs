// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Link;

public sealed record LinkModel
{
	public required string Label { get; init; }
	public required string Href { get; init; }

	/// <summary>Adds a trailing arrow icon and drops the underline until hover.</summary>
	public bool Arrow { get; init; }

	public bool External { get; init; }

	public string CssClass => Arrow ? "link link--arrow" : "link";
}
