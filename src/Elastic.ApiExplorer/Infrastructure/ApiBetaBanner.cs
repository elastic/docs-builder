// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>The beta notice above every API page since the Bump.sh cutover. Remove it with <c>_ApiBetaBanner</c> once the site leaves beta.</summary>
public sealed record ApiBetaBanner(string FeedbackUrl)
{
	private const string IssueReportUrl = "https://github.com/elastic/docs-content/issues/new?template=issue-report.yaml&labels=source:web";

	/// <summary>The same issue form as Report an issue on docs pages, with the page's public URL filled in when the build has one.</summary>
	public static ApiBetaBanner For(Uri? canonicalBaseUrl, string pageUrl)
	{
		if (canonicalBaseUrl is null || UrlPath.MakeAbsolute(canonicalBaseUrl, pageUrl) is not { } link)
			return new(IssueReportUrl);
		return new($"{IssueReportUrl}&link={Uri.EscapeDataString(link)}");
	}
}
