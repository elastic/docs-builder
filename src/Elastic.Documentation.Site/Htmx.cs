// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site;

/// <summary>
/// Boosted links use htmx's default whole-body swap with hx-preserve islands, so links no
/// longer need hx-select-oob. preload stays per-link because the preload extension ignores
/// ancestor attributes.
/// </summary>
public static class Htmx
{
	public const string Preload = "mousedown";
}
