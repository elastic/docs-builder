// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Banner;

public enum BannerKind
{
	Announcement,
	Info,
	Warning,
	Danger
}

/// <summary>A full-width announcement above the header, with an optional link.</summary>
public sealed record BannerModel
{
	public required string Message { get; init; }
	public BannerKind Kind { get; init; } = BannerKind.Announcement;
	public string? LinkLabel { get; init; }
	public string? LinkHref { get; init; }

	public string? Icon => Kind switch
	{
		BannerKind.Info => "help",
		BannerKind.Warning => "warningFilled",
		BannerKind.Danger => "errorFilled",
		_ => null
	};

	public string CssClass => Kind is BannerKind.Announcement ? "banner" : $"banner banner--{Kind.ToString().ToLowerInvariant()}";
}
