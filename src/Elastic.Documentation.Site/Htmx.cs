// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site;

/// <summary>
/// Boosted links swap <c>#main-container</c> (article + sidebar) so island heading/Overview
/// replace the ancestor tree. preload stays per-link because the preload extension ignores
/// ancestor attributes. Preserve islands include the global elastic-nav wrapper.
/// </summary>
public static class Htmx
{
	public const string Preload = "mousedown";
}
