// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Storybook;

public class StorybookViewModel : DirectiveViewModel
{
	public required string StoryUrl { get; init; }

	public required int Height { get; init; }

	public required string IframeTitle { get; init; }

	public bool HasBody { get; init; }

	public string HeightStyle => $"{Height}px";
}
