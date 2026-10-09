// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Method;

public static class MethodBadgeExamples
{
	public static ComponentSection Methods { get; } = new(
		"HTTP method",
		"A badge with a fixed tone per method, so an operation reads the same in the API catalog, the page header and the navigation tree. The small form replaces two older variants.",
		"method",
		[
			ComponentExample.Of("GET", _MethodBadge.Create, new MethodBadgeModel { Method = HttpVerb.Get }),
			ComponentExample.Of("POST", _MethodBadge.Create, new MethodBadgeModel { Method = HttpVerb.Post }),
			ComponentExample.Of("PUT", _MethodBadge.Create, new MethodBadgeModel { Method = HttpVerb.Put }),
			ComponentExample.Of("PATCH", _MethodBadge.Create, new MethodBadgeModel { Method = HttpVerb.Patch }),
			ComponentExample.Of("DELETE", _MethodBadge.Create, new MethodBadgeModel { Method = HttpVerb.Delete }),
			ComponentExample.Of("HEAD", _MethodBadge.Create, new MethodBadgeModel { Method = HttpVerb.Head }),
			ComponentExample.Of("Small", _MethodBadge.Create, new MethodBadgeModel { Method = HttpVerb.Delete, Small = true })
		]
	);
}
