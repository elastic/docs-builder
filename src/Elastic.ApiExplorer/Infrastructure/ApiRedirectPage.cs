// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Encodings.Web;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Static page that forwards a retired API URL to its replacement.</summary>
public static class ApiRedirectPage
{
	public static string Html(string targetUrl)
	{
		var href = WebUtility.HtmlEncode(targetUrl);
		var script = $"\"{JavaScriptEncoder.Default.Encode(targetUrl)}\"";
		return $"""
			<!DOCTYPE html>
			<html lang="en">
			<head>
			<meta charset="utf-8">
			<meta name="robots" content="noindex">
			<link rel="canonical" href="{href}">
			<meta http-equiv="refresh" content="0; url={href}">
			<script>window.location.replace({script} + window.location.search + window.location.hash);</script>
			<title>Moved</title>
			</head>
			<body><p>This page moved to <a href="{href}">{href}</a>.</p></body>
			</html>
			""";
	}
}
