// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.ApplicabilityBadges;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>The auth, availability and deprecated badges beside an operation's title or under its path.</summary>
/// <param name="DesignSystem">True when the design system look is on. The auth and deprecated badges then use the markup of its availability badge.</param>
public sealed record TitleBadgesModel(
	ApplicableToViewModel? Availability,
	bool Deprecated,
	bool DesignSystem,
	string? AuthText = null,
	string? AuthHref = null
)
{
	public bool Any => Availability is not null || Deprecated || AuthText is not null;
}

/// <summary>One text badge of that row. <paramref name="Href"/> makes it a link.</summary>
public sealed record TitleBadgeModel(string Text, bool DesignSystem, string? Href = null, bool Danger = false);
