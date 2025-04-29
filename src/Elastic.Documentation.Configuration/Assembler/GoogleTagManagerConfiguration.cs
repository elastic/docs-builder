// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Web;

namespace Elastic.Documentation.Configuration.Assembler;

public record GoogleTagManagerConfiguration
{
	public bool Enabled { get; init; }
	[MemberNotNullWhen(returnValue: true, nameof(Enabled))]
	public string? Id { get; init; }
	public string? Auth { get; init; }
	public string? Preview { get; init; }
	public string? CookiesWin { get; init; }

	public string QueryString()
	{
		var queryString = HttpUtility.ParseQueryString(string.Empty);
		if (Auth is not null)
			queryString.Add("gtm_auth", Auth);

		if (Preview is not null)
			queryString.Add("gtm_preview", Preview);

		if (CookiesWin is not null)
			queryString.Add("gtm_cookies_win", CookiesWin);

		return queryString.Count > 0 ? $"&{queryString}" : string.Empty;
	}
}
