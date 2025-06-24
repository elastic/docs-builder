// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Site;

public static class UrlHelper
{
	private static readonly KeyValuePair<string, string?>[] VersionParameters = [new("v", Htmx.VersionHash)];

	public static string AddVersionParameters(string uri) => AddQueryString(uri, VersionParameters);

	/// <summary>
	/// Append the given query keys and values to the URI.
	/// </summary>
	/// <param name="uri">The base URI.</param>
	/// <param name="queryString">A collection of name value query pairs to append.</param>
	/// <returns>The combined result.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="queryString"/> is <c>null</c>.</exception>
	public static string AddQueryString(
		string uri,
		IEnumerable<KeyValuePair<string, string?>> queryString)
	{
		ArgumentNullException.ThrowIfNull(uri);
		ArgumentNullException.ThrowIfNull(queryString);

		var anchorIndex = uri.IndexOf('#');
		var uriToBeAppended = uri.AsSpan();
		var anchorText = ReadOnlySpan<char>.Empty;
		// If there is an anchor, then the query string must be inserted before its first occurrence.
		if (anchorIndex != -1)
		{
			anchorText = uriToBeAppended.Slice(anchorIndex);
			uriToBeAppended = uriToBeAppended.Slice(0, anchorIndex);
		}

		var queryIndex = uriToBeAppended.IndexOf('?');
		var hasQuery = queryIndex != -1;

		var sb = new StringBuilder();
		_ = sb.Append(uriToBeAppended);
		foreach (var parameter in queryString)
		{
			if (parameter.Value == null)
				continue;

			_ = sb.Append(hasQuery ? '&' : '?')
			  .Append(UrlEncoder.Default.Encode(parameter.Key))
			  .Append('=')
			  .Append(UrlEncoder.Default.Encode(parameter.Value));
			hasQuery = true;
		}

		_ = sb.Append(anchorText);
		return sb.ToString();
	}
}

public static class Htmx
{
	private static readonly string Version =
		Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyInformationalVersionAttribute>()
			.FirstOrDefault()?.InformationalVersion ?? "0.0.0";

	public static readonly string VersionHash = ShortId.Create(Version);

	public static string GetHxSelectOob(bool hasSameTopLevelGroup) => hasSameTopLevelGroup ? "#content-container,#toc-nav" : "#main-container";
	public const string Preload = "mousedown";
	public const string HxSwap = "none";
	public const string HxPushUrl = "true";
	public const string HxIndicator = "#htmx-indicator";

	public static string GetHxAttributes(
		string targetUrl,
		bool hasSameTopLevelGroup = false,
		string? preload = Preload,
		string? hxSwapOob = null,
		string? hxSwap = HxSwap,
		string? hxPushUrl = HxPushUrl,
		string? hxIndicator = HxIndicator
	)
	{
		var hxGetUrl = UrlHelper.AddVersionParameters(targetUrl);

		var attributes = new StringBuilder();
		_ = attributes.Append($" hx-get={hxGetUrl}");
		_ = attributes.Append($" hx-select-oob={hxSwapOob ?? GetHxSelectOob(hasSameTopLevelGroup)}");
		_ = attributes.Append($" hx-swap={hxSwap}");
		_ = attributes.Append($" hx-push-url={hxPushUrl}");
		_ = attributes.Append($" hx-indicator={hxIndicator}");
		_ = attributes.Append($" preload={preload}");
		return attributes.ToString();
	}

	public static string GetNavHxAttributes(bool hasSameTopLevelGroup = false, string? preload = Preload)
	{
		var attributes = new StringBuilder();
		_ = attributes.Append($" hx-select-oob={GetHxSelectOob(hasSameTopLevelGroup)}");
		_ = attributes.Append($" preload={preload}");
		return attributes.ToString();
	}
}
