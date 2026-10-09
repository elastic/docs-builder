// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.PageActions;

public sealed record PageActionsModel
{
	public string? MarkdownUrl { get; init; }
	public string? ReportIssueUrl { get; init; }
	public string? EditUrl { get; init; }
	public string? ContributeUrl { get; init; }
}
