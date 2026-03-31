// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Extensions;

/// <summary>
/// Utility for joining URL path segments with forward slashes.
/// Unlike <see cref="Path.Join"/>, this always uses '/' and normalises
/// platform-specific directory separators, making it safe for URL construction.
/// </summary>
public static class UrlPath
{
	/// <summary>
	/// Joins two path segments with a single '/', normalising backslashes to forward slashes
	/// and trimming duplicate slashes at the join point.
	/// </summary>
	public static string Join(string left, string right) =>
		$"{left.Replace('\\', '/').TrimEnd('/')}/{right.Replace('\\', '/').TrimStart('/')}";

	/// <summary>
	/// Joins two path segments with a single '/', trimming duplicate slashes at the join point.
	/// Assumes both segments already use forward slashes.
	/// </summary>
	public static string JoinUrl(string left, string right) =>
		$"{left.TrimEnd('/')}/{right.TrimStart('/')}";
}
