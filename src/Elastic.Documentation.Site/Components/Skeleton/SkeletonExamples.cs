// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Skeleton;

public static class SkeletonExamples
{
	public static ComponentSection Skeletons { get; } = new(
		"Skeleton",
		"A placeholder for content that hasn't arrived. The shimmer is large motion, so it runs only when the reader hasn't asked for reduced motion.",
		"skeleton",
		[
			ComponentExample.Of("Text", _Skeleton.Create, new SkeletonModel { Lines = 3 }),
			ComponentExample.Of("With heading", _Skeleton.Create, new SkeletonModel { Heading = true, Lines = 2 })
		]
	);
}
