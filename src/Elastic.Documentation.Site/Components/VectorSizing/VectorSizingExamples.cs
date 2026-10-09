// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.VectorSizing;

public static class VectorSizingExamples
{
	public static ComponentSection Calculators { get; } = new(
		"Vector sizing calculator",
		"Estimates the disk and RAM a dense_vector field needs. The server renders the full form; script does the arithmetic and is the only part that needs JavaScript, so the calculator stays hidden until it runs.",
		"calc",
		[ComponentExample.Of("Calculator", _VectorSizing.Create, new VectorSizingModel { Id = "example-vector-sizing" })]
	);
}
