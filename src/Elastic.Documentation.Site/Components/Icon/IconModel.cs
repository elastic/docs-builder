// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Icon;

public enum IconSize
{
	Small,
	Medium,
	Large
}

public sealed record IconModel
{
	public required string Name { get; init; }
	public IconSize Size { get; init; } = IconSize.Medium;

	public string CssClass => Size switch
	{
		IconSize.Small => "icon icon--sm",
		IconSize.Large => "icon icon--lg",
		_ => "icon"
	};
}
