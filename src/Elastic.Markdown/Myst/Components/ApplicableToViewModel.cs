// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.FrontMatter;

namespace Elastic.Markdown.Myst.Components;

public class ApplicableToViewModel
{
	public required bool Inline { get; init; }
	public required ApplicableTo AppliesTo { get; init; }
}
