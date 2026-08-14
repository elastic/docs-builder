// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Hub;

public class LinkCardViewModel : HubDirectiveViewModel
{
	public required LinkCardData Data { get; init; }
	public required string? IconSvg { get; init; }

	/// <summary>Rendered as a titled link column inside an {explore} accordion.</summary>
	public bool IsColumn { get; init; }

}
