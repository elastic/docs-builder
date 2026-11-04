// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components;

/// <summary>Writes the <c>data-*</c> hooks a component hands to scripts. Scripts bind to these, never to the styling classes.</summary>
public static class ComponentData
{
	public static HtmlString Attributes(IReadOnlyDictionary<string, string>? data) =>
		new(
			string.Concat(
				(data ?? new Dictionary<string, string>()).Select(
					d => $" data-{WebUtility.HtmlEncode(d.Key)}" +
						(d.Value.Length == 0 ? string.Empty : $"=\"{WebUtility.HtmlEncode(d.Value)}\"")
				)
			)
		);
}
