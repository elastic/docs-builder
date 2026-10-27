// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Components.Prose;

public sealed record ProseModel
{
	/// <summary>The rendered article content.</summary>
	public required IHtmlContent Body { get; init; }
}
